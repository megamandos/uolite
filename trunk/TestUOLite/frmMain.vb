Imports UOLite2

Public Class frmMain
    Public WithEvents Client As New LiteClient
    Public Master As LiteClient.Serial = New LiteClient.Serial(0)
    Public Mount As UOLite2.LiteClient.Mobile = Nothing
    Public Password As String = "obey"

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click

        Dim ConnectResponse As String = Client.GetServerList(TextBox1.Text, TextBox2.Text, TextBox3.Text, TextBox4.Text)
        If ConnectResponse = "SUCCESS" Then
            Log("Connected to server: " & Client.LoginServerAddress & ":" & Client.LoginPort)
            TabControl1.SelectTab(1)
            Me.AcceptButton = SendButton
            CmdBox.Select()
        Else
            Log(ConnectResponse)
        End If

    End Sub

    Private Sub Client_LoginDenied(ByRef Reason As String) Handles Client.LoginDenied
        MsgBox(Reason)
    End Sub

    Private Sub Client_onCharacterListReceive(ByRef Client As LiteClient, ByVal CharacterList As System.Collections.ArrayList) Handles Client.onCharacterListReceive
        Client.ChooseCharacter(DirectCast(CharacterList.Item(0), CharListEntry).Name, DirectCast(CharacterList.Item(0), CharListEntry).Password, DirectCast(CharacterList.Item(0), CharListEntry).Slot)
    End Sub

    Private Sub Client_onCliLocSpeech(ByRef Client As UOLite2.LiteClient, ByVal Serial As UOLite2.LiteClient.Serial, ByVal BodyType As UShort, ByVal SpeechType As UOLite2.LiteClient.Enums.SpeechTypes, ByVal Hue As UShort, ByVal Font As UOLite2.LiteClient.Enums.Fonts, ByVal CliLocNumber As UInteger, ByVal Name As String, ByVal ArgsString As String) Handles Client.onCliLocSpeech
        Log("CliLoc: " & Name & " : " & Client.GetCliLocString(CliLocNumber))
    End Sub

    Private Sub Client_onError(ByRef Description As String) Handles Client.onError
        Log(Description)
    End Sub

#Region "Scavenger Stuff"
    Private Sub AddScavengerTypes()
        Client.Scavenger.AddType(UOLite2.LiteClient.Enums.Common.ItemTypes.GoldCoins)

    End Sub

#End Region

    Private Sub Client_onLoginComplete() Handles Client.onLoginComplete
        Me.Invoke(New _UpdatePlayerPosition(AddressOf UpdatePlayerPosition))
        Log("Login Complete")
        AddScavengerTypes()
        Client.Scavenger.Enabled = True
    End Sub

#Region "Finding the player's mount"

    Private WaitingForMount As Boolean = False

    Private Sub Client_onNewMobile(ByRef Client As UOLite2.LiteClient, ByVal Mobile As UOLite2.LiteClient.Mobile) Handles Client.onNewMobile
        If WaitingForMount = True Then
            Mount = Mobile
            WaitingForMount = False
            Client.Speak("Mount Found: " & Mount.Serial.ToRazorString)

            If Mount.Type = 791 Then
                'Announce the setting change.
                Client.Speak("Setting beetle mount as scavenger storage container.")

                'Set the container for the scavenger to use, setting the alternate container also enables use of an alternate container.
                'If these next two lines where here it would place scavenged items in the players backpack.
                Client.Scavenger.AlternateContainer.SetContainer(Mount.Serial)

                'Let the scavenger know that it has to dismount your player to access the storage container.
                Client.Scavenger.AlternateContainer.DismountToAccess = True
            End If

            Mount.DoubleClick()
        End If
    End Sub

#End Region

    Private Sub Client_onSkillUpdate(ByRef Client As UOLite2.LiteClient, ByRef OldSkill As UOLite2.LiteClient.Skill, ByRef NewSkill As UOLite2.LiteClient.Skill) Handles Client.onSkillUpdate
        If NewSkill.BaseValue > OldSkill.BaseValue Then
            'Client.Speak(NewSkill.Name & " has increased by " & CDec((NewSkill.BaseValue - OldSkill.BaseValue) / 10) & "!")
        End If
    End Sub

    Private Sub Client_onSpeech(ByRef Client As UOLite2.LiteClient, ByVal Serial As UOLite2.LiteClient.Serial, ByVal BodyType As UShort, ByVal SpeechType As UOLite2.LiteClient.Enums.SpeechTypes, ByVal Hue As UShort, ByVal Font As UOLite2.LiteClient.Enums.Fonts, ByVal Text As String, ByVal Name As String) Handles Client.onSpeech
        'Debug.WriteLine(Text)
        Log("SPEECH: " & Name & " : " & Text)

        If Master.Value = 0 OrElse Master = Serial Then
            Select Case LCase(Text.Split(" ")(0))
                Case Password
                    Client.Speak(Name & ", you are now my master, and I shall obey your every command.")
                    Master = Serial

                Case "me"
                    Client.Speak(Name & ", your serial is " & BitConverter.ToString(Serial.GetBytes) & ", or " & Serial.Value)

                Case "you"
                    Client.Speak(Name & ", my serial is " & BitConverter.ToString(Client.Player.Serial.GetBytes) & ", or " & Client.Player.Serial.Value)

                Case "where"
                    Client.Speak(Name & ", you are at " & Client.Mobiles.Mobile(Serial).X & "," & Client.Mobiles.Mobile(Serial).Y & " which is " & Client.Get2DDistance(Client.Player, Client.Mobiles.Mobile(Serial)) & " paces to the " & Client.GetDirection(Client.Player, Client.Mobiles.Mobile(Serial)).ToString & " of me.")

                Case "say"
                    Client.Speak(Text.Substring(Text.Split(" ")(0).Length + 1))

                Case "walk"
                    Select Case Text.Split(" ").Length
                        Case 1
                            Client.Walk(Client.Player.Direction)

                        Case 2
                            Select Case LCase(Text.Split(" ")(1))
                                Case "north"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.North)
                                Case "east"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.East)
                                Case "south"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.South)
                                Case "west"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.West)
                                Case "up"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.NorthWest)
                                Case "right"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.NorthEast)
                                Case "down"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.SouthEast)
                                Case "left"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.SouthWest)
                            End Select

                        Case 3
                            Select Case LCase(Text.Split(" ")(1))
                                Case "north"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.North, Text.Split(" ")(2))
                                Case "east"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.East, Text.Split(" ")(2))
                                Case "south"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.South, Text.Split(" ")(2))
                                Case "west"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.West, Text.Split(" ")(2))
                                Case "up"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.NorthWest, Text.Split(" ")(2))
                                Case "right"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.NorthEast, Text.Split(" ")(2))
                                Case "down"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.SouthEast, Text.Split(" ")(2))
                                Case "left"
                                    Client.Walk(UOLite2.LiteClient.Enums.Direction.SouthWest, Text.Split(" ")(2))
                            End Select


                    End Select
                Case "follow"
                    Client.Follow(Serial)

                Case "come"
                    Client.Walk(Client.GetDirection(Client.Player, Client.Mobiles.Mobile(Serial)))

                Case "stay"
                    Client.StopFollowing()

                Case "hide"
                    Client.Skills(UOLite2.LiteClient.Enums.Skills.Hiding).Use()

                Case "skill"
                    If Text.Split(" ").Length = 2 Then
                        Dim skillnum As UOLite2.LiteClient.Enums.Skills = Text.Split(" ")(1)
                        Client.Speak("Skill: " & Client.Skills(skillnum).Name)
                        Client.Speak("Value: " & Math.Round(CDec(Client.Skills(skillnum).Value / 10), 1))
                        Client.Speak("Base Value: " & Math.Round(CDec(Client.Skills(skillnum).BaseValue / 10), 1))
                        Client.Speak("Lock: " & Client.Skills(skillnum).Lock.ToString)
                        Client.Speak("Cap: " & Math.Round(CDec(Client.Skills(skillnum).Cap / 10), 1))
                    End If

                Case "ping"
                    Client.Speak("My packet round trip time is " & Client.Latency & "ms")

                Case "mcount"
                    Client.Speak("I know of " & Client.Mobiles.Count & " mobiles.")

                Case "icount"
                    Client.Speak("I know of " & Client.Items.Count & " items.")

                Case "weight"
                    Client.Speak(" My backpack weighs " & Client.Player.Weight & " stones, although I can carry " & Client.Player.MaxWeight & " stones.")

                Case "contents"
                    Client.Speak("I have " & Client.Player.Layers.BackPack.Contents.Count & " items in my backpack.")

                    For Each i As UOLite2.LiteClient.Item In Client.Player.Layers.BackPack.Contents.Items
                        Client.Speak("Item: " & i.Serial.ToRazorString & "," & i.Type & "," & i.Amount)
                    Next

                Case "dropall"

                    Client.Scavenger.Enabled = False

                    For Each i As UOLite2.LiteClient.Item In Client.Player.Layers.BackPack.Contents.Items
                        i.Drop()
                    Next


                Case "ts"
                    Client.Scavenger.Toggle(False)

                Case "findmount"
                    If Client.Player.IsMounted Then
                        'Let the rest of the code know to expect a mobile to show up, which is my mount.
                        WaitingForMount = True

                        'Doubleclick myself to dismount
                        Client.Player.DoubleClick()
                    End If

                Case "mount"
                    If Mount IsNot Nothing Then
                        Mount.DoubleClick()
                    End If

                Case "dmount", "dismount"
                    _Client.Player.DoubleClick()
                Case "sc"
                    Client.Speak("Container: " & Client.Scavenger.AlternateContainer.Container.ToRazorString)

            End Select
        End If

    End Sub

    Private Sub SendButton_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SendButton.Click
        Select Case LCase(CmdBox.Text.Split(" ")(0))
            Case "say"
                Client.Speak(CmdBox.Text.Substring(CmdBox.Text.Split(" ")(0).Length + 1), LiteClient.Enums.SpeechTypes.Regular)
                CmdBox.Clear()
            Case "send"
                Client.Send(CmdBox.Text.Substring(CmdBox.Text.Split(" ")(0).Length + 1))
        End Select
    End Sub

#Region "Logging Code"

    Public Delegate Sub ConsoleWrite(ByRef Text As String)

    Private Sub Client_RecievedServerList() Handles Client.RecievedServerList
        Client.ChooseServer(0)
    End Sub

    Private Sub ConsoleLog(ByRef Text As String)
        ConsoleBox.SuspendLayout()
        ConsoleBox.AppendText(Text)
        ConsoleBox.ResumeLayout()
    End Sub

    Private Sub Log(ByRef Text As String)
        Dim args(0) As Object
        args(0) = Text & vbNewLine
        Me.Invoke(New ConsoleWrite(AddressOf ConsoleLog), args)
    End Sub

#End Region

#Region "Movement"

    Private Sub Client_onPlayerMove(ByRef Client As UOLite2.LiteClient) Handles Client.onPlayerMove
        Me.Invoke(New _UpdatePlayerPosition(AddressOf UpdatePlayerPosition))
    End Sub

    Private Delegate Sub _UpdatePlayerPosition()

    Private Sub UpdatePlayerPosition()
        lbl_posx.Text = Client.Player.X
        lbl_posy.Text = Client.Player.Y
        lbl_posz.Text = Client.Player.Z
        lbl_Direction.Text = Client.Player.Direction.ToString
    End Sub

    Private Sub Button8_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button8.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.North)
    End Sub

    Private Sub Button6_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button6.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.South)
    End Sub

    Private Sub Button5_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button5.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.East)
    End Sub

    Private Sub Button10_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button10.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.West)
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.NorthWest)
    End Sub

    Private Sub Button7_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button7.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.SouthEast)
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.SouthWest)
    End Sub

    Private Sub Button9_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button9.Click
        Client.Walk(UOLite2.LiteClient.Enums.Direction.NorthEast)
    End Sub

    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        Dim packet() As Byte = {7, &H41, &HD6, &H85, &HEF, 0, 1}
        Client.Send(packet)
    End Sub

#End Region

End Class