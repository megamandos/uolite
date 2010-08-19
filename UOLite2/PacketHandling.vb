' This is for handling the packets as they are received.
' The process is done in serveral steps.
' 1. The LARGE packet is received, compressed
'   The "LARGE" packet is how the packets are actually
'   received by the client from the server, which is 
'   actually several packets compressed using huffman
'   compression.
' 2. A thread is started by the NetworkStream "_GameStream"
'   The thread adds the packet (compressed) to the packet buffer.
' 3. The thread then checks to see if the "PacketHandlerThread"
'   is awake and processing packets. If it isnt, then it wakes it up.
' 4. The "PacketHandlerThread" decompresses a "Game Packet" from the 
'   GameBuffer.
' 5. The packet is then transformed from a byte array to a packet
'   class by "BuildPacket".
' 6. Then the packet is handled by PacketHandling approprietly.
'
' So as a summary: A packet is recieved, a thread is started, that thread
' adds the packet to a buffer and starts another thread, that new thread
' handles a single packet, then checks the buffer for more packets, then
' handles those, and checks again, if no more packets are in the buffer the
' thread shuts down (to be restarted later when the server sends more packets).

Imports System.IO, System.Threading, System.Net.Sockets

'Saves the packets to a file, packetlog.txt.
#Const LogPackets = False

'Prints out when data is recieved and when it is handled, and reports on buffer size.
#Const TrafficStats = False

'Prints the packet contents to the debug output.
#Const DebugGamePackets = True

'Prints the contents of login packets to the debug output.
#Const DebugLoginPackets = False

Partial Class LiteClient

    Const PacketSize As Integer = 1024 * 32 '32kB packet buffer.
    Private Shared BufferSize As UInteger = 1024 * 1024 * 5 '5 Megabytes
    Private Shared GameBuffer As New CircularBuffer(Of Byte)(BufferSize)
    Private PacketHandlerThread As New Thread(AddressOf HandleBuffer)

#If LogPackets Then
    Private PacketLog As System.IO.StreamWriter = File.CreateText(Application.StartupPath & "\packets.log")
#End If

    ''' <summary>
    ''' Called when a Packet arrives on this client.
    ''' </summary>
    ''' <param name="Client">Client on which the packet was received</param>
    ''' <param name="bytes">The received packet</param>
    Public Event onPacketReceive(ByRef Client As LiteClient, ByRef bytes() As Byte)

    Private Sub GameRecieve(ByVal ar As IAsyncResult)
        '--Retreive array of bytes
        Dim bytes() As Byte = ar.AsyncState

        '--Get number of bytes received and also clean up resources that was used from beginReceive
        Dim numBytes As Int32 = _GameStream.EndRead(ar)

        '--Did we receive anything?
        If numBytes > 0 Then
            'Resize the array to match the number of bytes received. Also keep the current data
            ReDim Preserve bytes(numBytes - 1)

            'Write compressed data to the buffer.
            GameBuffer.Write(bytes)

            If GameBuffer.Size > BufferSize - 1 Then Throw New ApplicationException("Game Buffer Overflow! This should NEVER happen, please report this IMMEDIATELY.")

#If TrafficStats Then
            Debug.WriteLine("Recieved " & bytes.Length & " compressed bytes from the server.")
#End If

            If PacketHandlerThread.IsAlive = False Then
                'wake up the handler thead.
                PacketHandlerThread = New System.Threading.Thread(AddressOf HandleBuffer)
                PacketHandlerThread.Start()
            End If

        End If

        '--Are we still connected?
        If _GameClient.Connected = False Then
            'Do something about being disconnected?
        Else
            '--Yes, then resize bytes to packet size
            ReDim bytes(PacketSize - 1)

            '--Call BeginReceive again, catching any error
            Try
                _GameStream.BeginRead(bytes, 0, bytes.Length, AddressOf GameRecieve, bytes)
            Catch ex As Exception
                'Deal with the disconnect?
            End Try
        End If
    End Sub
    ' |
    ' |
    ' V
    Private Sub HandleBuffer()
        'Continue to handle packets until there isnt any whole packets left in the buffer.
        While HandleSinglePacket()
        End While
    End Sub
    ' |<------A
    ' |       |
    ' V       |
    Private Function HandleSinglePacket() As Boolean
        ProcessingPacket = True
        Dim Packet() As Byte

        'Attempt to decompress a single packet.
        'If the decompression algorithm returned nothing then display a warning and exit.
        If DecompressSinglePacket(True) Is Nothing Then
#If DebugGamePackets Then
            Debug.WriteLine("WARNING: Not enough data in the buffer for this whole packet, or the buffer is empty! Waiting for next packet from the server!")
#End If
            ProcessingPacket = False
            Return False
        Else
            'otherwise, decompress the packet again, this time, no peeking!
            Packet = DecompressSinglePacket(False)
        End If

#If LogPackets Then
        PacketLog.WriteLine("PACKET: " & BitConverter.ToString(Packet))
#End If

#If DebugGamePackets Then
        Debug.WriteLine("PACKET: " & BitConverter.ToString(Packet))
#End If

        'Raise the onPacketReceive event
        RaiseEvent onPacketReceive(Me, Packet)

        'Build the actual packet object and send it to get handled.
        PacketHandling(BuildPacket(Packet))

#If TrafficStats Then
        Debug.WriteLine(Packet.Length & " byte packet handled, " & GameBuffer.Size & " bytes left in buffer.")
#End If

        'Let everything else know that you finished handling this packet.
        ProcessingPacket = False

        Return True
    End Function '<---------------------
    ' |                                                                        A
    ' |                                                                        |
    ' V                                                                        |
    Private Function BuildPacket(ByRef packetbuffer As Byte()) As Packet
        Try

            Select Case DirectCast(packetbuffer(0), Enums.PacketType)
                Case Enums.PacketType.TakeObject
                    Dim k As New Packets.TakeObject(packetbuffer)
                    _ItemInHand = k.Serial
                    Return k

                Case Enums.PacketType.DropObject
                    Return New Packets.DropObject(packetbuffer)

                Case Enums.PacketType.TextUnicode
                    Return New Packets.UnicodeText(packetbuffer)

                Case Enums.PacketType.SpeechUnicode
                    Return New Packets.UnicodeSpeechPacket(packetbuffer)

                Case Enums.PacketType.NakedMOB
                    Return New Packets.NakedMobile(packetbuffer)

                Case Enums.PacketType.EquippedMOB
                    Return New Packets.EquippedMobile(packetbuffer)

                Case Enums.PacketType.FatHealth
                    Return New Packets.FatHealth(packetbuffer)

                Case Enums.PacketType.HPHealth
                    Return New Packets.HPHealth(packetbuffer)

                Case Enums.PacketType.ManaHealth
                    Return New Packets.ManaHealth(packetbuffer)

                Case Enums.PacketType.DeathAnimation
                    Return New Packets.DeathAnimation(packetbuffer)

                Case Enums.PacketType.Destroy
                    Return New Packets.Destroy(packetbuffer)

                Case Enums.PacketType.MobileStats
                    Return New Packets.MobileStats(packetbuffer)

                Case Enums.PacketType.EquipItem
                    Return New Packets.EquipItem(packetbuffer)

                Case Enums.PacketType.ContainerContents
                    Return New Packets.ContainerContents(packetbuffer)

                Case Enums.PacketType.ObjecttoObject
                    Return New Packets.ObjectToObject(packetbuffer)

                Case Enums.PacketType.ShowItem
                    Return New Packets.ShowItem(packetbuffer)

                Case Enums.PacketType.Target
                    Return New Packets.Target(packetbuffer)

                Case Enums.PacketType.DoubleClick
                    Return New Packets.Doubleclick(packetbuffer)

                Case Enums.PacketType.SingleClick
                    Return New Packets.Singleclick(packetbuffer)

                Case Enums.PacketType.Text
                    Return New Packets.Text(packetbuffer)

                Case Enums.PacketType.LoginConfirm
                    Return New Packets.LoginConfirm(packetbuffer)

                Case Enums.PacketType.HealthBarStatusUpdate
                    Return New Packets.HealthBarStatusUpdate(packetbuffer)

                Case Enums.PacketType.CompressedGump
                    Return New Packets.CompressedGump(packetbuffer)

                Case Enums.PacketType.GenericCommand

                    Select Case DirectCast(CUShort(packetbuffer(4)), Enums.BF_Sub_Commands)

                        Case Enums.BF_Sub_Commands.ContextMenuRequest
                            Return New Packets.ContextMenuRequest(packetbuffer)

                        Case Enums.BF_Sub_Commands.ContextMenuResponse
                            Return New Packets.ContextMenuResponse(packetbuffer)

                        Case Enums.BF_Sub_Commands.AddWalkKey
                            Return New Packets.AddWalkKey(packetbuffer)

                        Case Enums.BF_Sub_Commands.FastWalk
                            Return New Packets.FastWalk(packetbuffer)

                        Case Else
                            Dim j As New Packet(packetbuffer(0))
                            j._Data = packetbuffer
                            j._size = packetbuffer.Length
                            Return j 'dummy until we have what we need
                    End Select

                Case Enums.PacketType.BlockMovement
                    Return New Packets.BlockMovement(packetbuffer)

                Case Enums.PacketType.AcceptMovement_ResyncRequest
                    Return New Packets.AcceptMovement_ResyncRequest(packetbuffer)

                Case Enums.PacketType.Teleport
                    Return New Packets.Teleport(packetbuffer)

                Case Enums.PacketType.HuePicker
                    Return New Packets.HuePicker(packetbuffer)

                Case Enums.PacketType.LocalizedText
                    Return New Packets.LocalizedText(packetbuffer)

                Case Enums.PacketType.LoginComplete
                    Return New Packets.LoginComplete(packetbuffer)

                Case Enums.PacketType.Skills
                    Return New Packets.Skills(packetbuffer, Me)

                Case Enums.PacketType.CharacterList
                    Return New Packets.CharacterList(packetbuffer)

                Case Else
                    Dim j As New Packet(packetbuffer(0))
                    j._Data = packetbuffer
                    j._size = packetbuffer.Length
                    Return j 'dummy until we have what we need
            End Select

        Catch ex As Exception
            Dim k() As Byte = {0}
            Dim j As New Packet(k(0))

            j._Data = packetbuffer
            j._size = packetbuffer.Length

            Return j
        End Try

    End Function ' |
    ' |                                                                        |
    ' |                                                                        |
    ' V  --------------------------------------------------------------------->|
    ''' <summary>Handles a packet however it needs to be handled.</summary>
    ''' <param name="currentpacket">The packet to process.</param>
    Private Sub PacketHandling(ByRef currentpacket As Packet)
        Select Case currentpacket.Type
            Case Enums.PacketType.ClientVersion
                'Respond with 0xBD packet.
                Dim VerPacket(11) As Byte
                VerPacket(0) = 189

                'Packet Size
                VerPacket(1) = 0
                VerPacket(2) = 12

                '6.0.13.0 Version string with Null terminator.
                VerPacket(3) = 54 '6
                VerPacket(4) = 46 '.
                VerPacket(5) = 48 '0
                VerPacket(6) = 46 '.
                VerPacket(7) = 49 '1
                VerPacket(8) = 51 '3
                VerPacket(9) = 46 '.
                VerPacket(10) = 48 '0
                VerPacket(11) = 0 'Null Terminator

                'Respond with version string.
                _GameStream.Write(VerPacket, 0, VerPacket.Length)

            Case Enums.PacketType.CharacterList
                _CharacterList = DirectCast(currentpacket, Packets.CharacterList).CharacterList
                RaiseEvent onCharacterListReceive(Me, _CharacterList)

#If DebugGamePackets Then
                Debug.WriteLine("Character List")
#End If
            Case Enums.PacketType.MobileStats
                'We already know now that the mobile exists, because this packet isnt sent until after the MOB is created
                'So there is no need to check for the existance of the MOB. Just send the packet to the mobile for it to update itself.
                'This is done through direct casts and hash tables, so its REALLY fast.
                _Mobiles.Mobile(DirectCast(currentpacket, Packets.MobileStats).Serial).HandleUpdatePacket(DirectCast(currentpacket, Packets.MobileStats))

#If DebugGamePackets Then
                Debug.WriteLine("Mobile Stats")
#End If
            Case Enums.PacketType.HPHealth
                _Mobiles.Mobile(DirectCast(currentpacket, Packets.HPHealth).Serial).HandleUpdatePacket(DirectCast(currentpacket, Packets.HPHealth))

#If DebugGamePackets Then
                Debug.WriteLine("HP Health")
#End If

            Case Enums.PacketType.FatHealth
                _Mobiles.Mobile(DirectCast(currentpacket, Packets.FatHealth).Serial).HandleUpdatePacket(DirectCast(currentpacket, Packets.FatHealth))

#If DebugGamePackets Then
                Debug.WriteLine("Fat Health")
#End If

            Case Enums.PacketType.ManaHealth
                _Mobiles.Mobile(DirectCast(currentpacket, Packets.ManaHealth).Serial).HandleUpdatePacket(DirectCast(currentpacket, Packets.ManaHealth))

#If DebugGamePackets Then
                Debug.WriteLine("Mana Health")
#End If

            Case Enums.PacketType.NakedMOB
                _Mobiles.AddMobile(DirectCast(currentpacket, Packets.NakedMobile))

#If DebugGamePackets Then
                Debug.WriteLine("Naked MOB")
#End If

            Case Enums.PacketType.EquippedMOB
                _Mobiles.AddMobile(DirectCast(currentpacket, Packets.EquippedMobile))

#If DebugGamePackets Then
                Debug.WriteLine("Equipped MOB")
#End If
                'Mobile is approaching.
                RaiseEvent onNewMobile(Me, _Mobiles.Mobile(DirectCast(currentpacket, Packets.EquippedMobile).Serial))

            Case Enums.PacketType.DeathAnimation
                _Mobiles.Mobile(DirectCast(currentpacket, Packets.DeathAnimation).Serial).HandleDeathPacket(DirectCast(currentpacket, Packets.DeathAnimation))

#If DebugGamePackets Then
                Debug.WriteLine("Death Animation")
#End If

            Case Enums.PacketType.Destroy
                RemoveObject(DirectCast(currentpacket, Packets.Destroy).Serial)

#If DebugGamePackets Then
                Debug.WriteLine("Destroy Object")
#End If

            Case Enums.PacketType.EquipItem
                _Mobiles.Mobile(DirectCast(currentpacket, Packets.EquipItem).Container).HandleUpdatePacket(DirectCast(currentpacket, Packets.EquipItem))

#If DebugGamePackets Then
                Debug.WriteLine("Equip Item")
#End If

            Case Enums.PacketType.ContainerContents
                Items.Add(DirectCast(currentpacket, Packets.ContainerContents))

#If DebugGamePackets Then
                Debug.WriteLine("Container Contents")
#End If

            Case Enums.PacketType.ObjecttoObject
                Items.Add(DirectCast(currentpacket, Packets.ObjectToObject))

#If DebugGamePackets Then
                Debug.WriteLine("Object To Object")
#End If

            Case Enums.PacketType.ShowItem
                Items.Add(DirectCast(currentpacket, Packets.ShowItem))

                Scavenger.CheckForPickup(DirectCast(currentpacket, Packets.ShowItem).Serial)

#If DebugGamePackets Then
                Debug.WriteLine("Show Item")
#End If

            Case Enums.PacketType.Target
#If DebugGamePackets Then
                Debug.WriteLine("Target Request")
#End If

            Case Enums.PacketType.HuePicker
#If DebugGamePackets Then
                Debug.WriteLine("Hue Picker")
#End If

            Case Enums.PacketType.LoginConfirm
                'Make a new playerclass
                Dim pl As New Mobile(Me, DirectCast(currentpacket, Packets.LoginConfirm).Serial)

                'Apply the packet's info to the new playerclass
                pl._Type = DirectCast(currentpacket, Packets.LoginConfirm).BodyType
                pl._X = DirectCast(currentpacket, Packets.LoginConfirm).X
                pl._Y = DirectCast(currentpacket, Packets.LoginConfirm).Y
                pl._Z = DirectCast(currentpacket, Packets.LoginConfirm).Z
                pl._Direction = DirectCast(currentpacket, Packets.LoginConfirm).Direction

                _Player = pl

                'Cast the player as a mobile and add it to the mobile list.
                _Mobiles.AddMobile(pl)

#If DebugGamePackets Then
                Debug.WriteLine("Login Confirm")
#End If

                RaiseEvent onLoginConfirm(Player)
            Case Enums.PacketType.HealthBarStatusUpdate
                _Mobiles.Mobile(DirectCast(currentpacket, Packets.HealthBarStatusUpdate).Serial).HandleUpdatePacket(DirectCast(currentpacket, Packets.HealthBarStatusUpdate))

#If DebugGamePackets Then
                Debug.WriteLine("Health Bar Status Update")
#End If

            Case Enums.PacketType.LocalizedText
                RaiseEvent onCliLocSpeech(Me, DirectCast(currentpacket, Packets.LocalizedText).Serial, _
                                          DirectCast(currentpacket, Packets.LocalizedText).BodyType, _
                                           DirectCast(currentpacket, Packets.LocalizedText).SpeechType, _
                                            DirectCast(currentpacket, Packets.LocalizedText).Hue, _
                                            DirectCast(currentpacket, Packets.LocalizedText).Font, _
                                            DirectCast(currentpacket, Packets.LocalizedText).CliLocNumber, _
                                            DirectCast(currentpacket, Packets.LocalizedText).Name, _
                                            DirectCast(currentpacket, Packets.LocalizedText).ArgString)

#If DebugGamePackets Then
                Debug.WriteLine("Localized Text")
#End If

            Case Enums.PacketType.Text
                RaiseEvent onSpeech(Me, DirectCast(currentpacket, Packets.Text).Serial, _
                                          DirectCast(currentpacket, Packets.Text).BodyType, _
                                          DirectCast(currentpacket, Packets.Text).SpeechType, _
                                          DirectCast(currentpacket, Packets.Text).TextHue, _
                                          DirectCast(currentpacket, Packets.Text).TextFont, _
                                          DirectCast(currentpacket, Packets.Text).Text, _
                                          DirectCast(currentpacket, Packets.Text).Name)

#If DebugGamePackets Then
                Debug.WriteLine("Text")
#End If

            Case Enums.PacketType.TextUnicode
                RaiseEvent onSpeech(Me, DirectCast(currentpacket, Packets.UnicodeText).Serial, _
                      DirectCast(currentpacket, Packets.UnicodeText).Body, _
                      DirectCast(currentpacket, Packets.UnicodeText).Mode, _
                      DirectCast(currentpacket, Packets.UnicodeText).Hue, _
                      DirectCast(currentpacket, Packets.UnicodeText).Font, _
                      DirectCast(currentpacket, Packets.UnicodeText).Text, _
                      DirectCast(currentpacket, Packets.UnicodeText).Name)

#If DebugGamePackets Then
                Debug.WriteLine("Unicode Text")
#End If

            Case Enums.PacketType.LoginComplete
                'Start sending keepalive packets.
                BF24Ticker.Enabled = True

                'Make the action buffer for doubleclicks and pickups.
                ActionBuffer = New ActionBufferClass(Me)

                'send special packet??
                _GameStream.Write(specialpacket, 0, specialpacket.Length)

                'Request skills
                RequestSkills()

                'Double click my backpack.
                Player.Layers.BackPack.DoubleClick()

                'Check surroundings for scavengable items.
                Scavenger.CheckSurroundings()

                RaiseEvent onLoginComplete()

#If DebugGamePackets Then
                Debug.WriteLine("Login Complete")
#End If

            Case Enums.PacketType.AcceptMovement_ResyncRequest
                AcceptMovement(DirectCast(currentpacket, Packets.AcceptMovement_ResyncRequest))

            Case Enums.PacketType.Teleport
                HandleTeleport(DirectCast(currentpacket, Packets.Teleport))

                'Check surroundings for scavengable items.
                Scavenger.CheckSurroundings()

            Case Enums.PacketType.BlockMovement
                MovementBlocked(DirectCast(currentpacket, Packets.BlockMovement))

                'Check surroundings for scavengable items.
                'Scavenger.CheckSurroundings()

            Case Enums.PacketType.Skills
                HandleSkillPacket(DirectCast(currentpacket, Packets.Skills))

            Case Enums.PacketType.CompressedGump
                'Debug.WriteLine(DirectCast(currentpacket, Packets.CompressedGump).DecompressedGumpData)
                'Debug.WriteLine(DirectCast(currentpacket, Packets.CompressedGump).DecompressedTextData)

                Dim retgump As New Gump(currentpacket, Me)

                RaiseEvent onNewGump(Me, retgump)

            Case Enums.PacketType.GenericCommand
                Select Case currentpacket.Data(4)
                    Case Enums.BF_Sub_Commands.FastWalk
                        EnableFastWalk(DirectCast(currentpacket, Packets.FastWalk))

                    Case Enums.BF_Sub_Commands.AddWalkKey
                        AddFastWalkKey(DirectCast(currentpacket, Packets.AddWalkKey))

                End Select

            Case Else

#If DebugGamePackets Then
                Debug.WriteLine("Unhandled Packet Type: " & currentpacket.Type.ToString)
#End If

        End Select
    End Sub

    Public Overloads Sub Send(ByRef Packet As Packet)
        Send(Packet.Data)
    End Sub

    Public Overloads Sub Send(ByRef Packet As String)
        Dim bytes(Packet.Length / 3 - 1) As Byte
        Dim x As UInteger = 0

        For i As Integer = 0 To Packet.Length Step 3
            Select Case Packet(i)
                Case "0"
                    bytes(x) = 0 * 16
                Case "1"
                    bytes(x) = 1 * 16
                Case "2"
                    bytes(x) = 2 * 16
                Case "3"
                    bytes(x) = 3 * 16
                Case "4"
                    bytes(x) = 4 * 16
                Case "5"
                    bytes(x) = 5 * 16
                Case "6"
                    bytes(x) = 6 * 16
                Case "7"
                    bytes(x) = 7 * 16
                Case "8"
                    bytes(x) = 8 * 16
                Case "9"
                    bytes(x) = 9 * 16
                Case "A", "a"
                    bytes(x) = 10 * 16
                Case "B", "b"
                    bytes(x) = 11 * 16
                Case "C", "c"
                    bytes(x) = 12 * 16
                Case "D", "d"
                    bytes(x) = 13 * 16
                Case "E", "e"
                    bytes(x) = 14 * 16
                Case "F", "f"
                    bytes(x) = 15 * 16
            End Select

            Select Case Packet(i + 1)
                Case "0"
                    bytes(x) += 0
                Case "1"
                    bytes(x) += 1
                Case "2"
                    bytes(x) += 2
                Case "3"
                    bytes(x) += 3
                Case "4"
                    bytes(x) += 4
                Case "5"
                    bytes(x) += 5
                Case "6"
                    bytes(x) += 6
                Case "7"
                    bytes(x) += 7
                Case "8"
                    bytes(x) += 8
                Case "9"
                    bytes(x) += 9
                Case "A", "a"
                    bytes(x) += 10
                Case "B", "b"
                    bytes(x) += 11
                Case "C", "c"
                    bytes(x) += 12
                Case "D", "d"
                    bytes(x) += 13
                Case "E", "e"
                    bytes(x) += 14
                Case "F", "f"
                    bytes(x) += 15
            End Select

            x += 1

        Next

        Send(bytes)

    End Sub

    Public Overloads Sub Send(ByRef Packet() As Byte)


        'TODO: This might have to write to a threadsafe buffer, but we will see.
        If _GameClient.Connected Then

#If DebugGamePackets Then
            Debug.WriteLine("SENDING: " & BitConverter.ToString(Packet))
#End If

            If _Encrypted Then
                GameCryptBytes(Packet)
            End If

            Try
                _GameStream.Write(Packet, 0, Packet.Length)
            Catch ex As Exception
                RaiseEvent onConnectionLoss(Me)
            End Try

        ElseIf _LoginClient.Connected Then

#If DebugLoginPackets Then
            Debug.WriteLine("SENDING LOGIN PACKET: " & BitConverter.ToString(Packet))
#End If

            If _Encrypted Then
                LoginCryptBytes(Packet)
            End If

            _LoginStream.Write(Packet, 0, Packet.Length)
        Else
            Throw New ApplicationException("Unable to send packet, you are not connected!")
        End If

    End Sub

    Private Sub LoginRecieve(ByVal ar As IAsyncResult)
        '--Retreive array of bytes
        Dim bytes() As Byte = ar.AsyncState

        '--Get number of bytes received and also clean up resources that was used from beginReceive
        Dim numBytes As Int32 = _LoginStream.EndRead(ar)

        '--Did we receive anything?
        If numBytes > 0 Then
            '--Resize the array to match the number of bytes received. Also keep the current data
            ReDim Preserve bytes(numBytes - 1)

            If _Encrypted Then LoginCryptBytes(bytes)

#If DebugLoginPackets Then
            Debug.WriteLine("LOGIN PACKET: " & BitConverter.ToString(bytes))
#End If

            Select Case bytes(0)
                Case 168 'Server List

                    'Populate and display the server list.
                    Dim i As Short = 0
                    Dim s As Integer = 0
                    Dim ShardCountBytes(1) As Byte
                    ShardCountBytes(0) = bytes(5)
                    ShardCountBytes(1) = bytes(4)
                    Dim ShardCount As Short = BitConverter.ToInt16(ShardCountBytes, 0)

                    Dim NameBytes(31) As Byte
                    Dim svrlist(ShardCount - 1) As GameServerInfo

                    For i = 0 To ShardCount - 1
                        'Get The Name
                        For s = i + 8 To i + 39
                            NameBytes(s - (i + 8)) = bytes((i * 40) + s)
                        Next

                        'Create the gameserverinfo object.
                        svrlist(i) = New GameServerInfo(System.Text.Encoding.ASCII.GetString(NameBytes).Replace(Chr(0), ""), bytes(i + 45) & "." & bytes(i + 44) & "." & bytes(i + 43) & "." & bytes(i + 42), bytes(i + 40))

                    Next

                    'copy the game server list into the GameServerList...
                    _GameServerList = svrlist

                    RaiseEvent onRecievedServerList(ServerList)

                Case 140

                    _GameServerAddress = bytes(1) & "." & bytes(2) & "." & bytes(3) & "." & bytes(4)

                    Dim GameServerPortBytes(1) As Byte
                    GameServerPortBytes(0) = bytes(6)
                    GameServerPortBytes(1) = bytes(5)

                    _GameServerPort = BitConverter.ToInt16(GameServerPortBytes, 0)

                    Dim AccountUIDBytes() As Byte = {bytes(10), bytes(9), bytes(8), bytes(7)}
                    _EncryptionGamePlaySeed = BitConverter.ToUInt32(AccountUIDBytes, 0)

                    GenerateGamePlayKeys(_EncryptionGamePlaySeed)

                    Try
                        _GameClient = New TcpClient
                        _GameClient.ReceiveBufferSize = PacketSize
                        _GameClient.Connect(GameServerAddress, GameServerPort)
                        _GameStream = _GameClient.GetStream
                    Catch ex As Exception
                        RaiseEvent onError("Failed to connect to game server: " & ex.Message)
                    End Try

                    Dim EncBytes() As Byte = BitConverter.GetBytes(_EncryptionGamePlaySeed)
                    Dim EncACK(3) As Byte
                    EncACK(0) = EncBytes(3)
                    EncACK(1) = EncBytes(2)
                    EncACK(2) = EncBytes(1)
                    EncACK(3) = EncBytes(0)
                    _GameStream.Write(EncACK, 0, EncACK.Length)

                    'As soon as it connects, send the username,password, and encryption key to request the character list.
                    Dim CharListReq(64) As Byte
                    Dim NameBytes() As Byte = GetBytesFromString(30, _Username)
                    Dim PassBytes() As Byte = GetBytesFromString(30, _Password)

                    CharListReq(0) = 145

                    'Add the Encryption Key (accountuid)
                    CharListReq(1) = EncBytes(3)
                    CharListReq(2) = EncBytes(2)
                    CharListReq(3) = EncBytes(1)
                    CharListReq(4) = EncBytes(0)

                    Dim i As Integer = 0

                    'Add the account name
                    For i = 5 To 34
                        CharListReq(i) = NameBytes(i - 5)
                    Next

                    'Add the password
                    For i = 35 To 64
                        CharListReq(i) = PassBytes(i - 35)
                    Next

                    _GameStream.Write(CharListReq, 0, CharListReq.Length)

                    'Set up asynchronous reading.
                    Dim RecBuffer(PacketSize) As Byte
                    _GameStream.BeginRead(RecBuffer, 0, RecBuffer.Length, AddressOf GameRecieve, RecBuffer)

                    'Close the connection to the server
                    _LoginClient.Close()
                    _LoginStream.Close()
                    Exit Sub

                Case 130
                    Select Case bytes(1)
                        Case 0
                            RaiseEvent onLoginDenied("Invalid Username/Password")
                        Case 1
                            RaiseEvent onLoginDenied("Someone is already using this account!")
                        Case 2
                            RaiseEvent onLoginDenied("Your account has been locked!")
                        Case 3
                            RaiseEvent onLoginDenied("Your account credentials are invalid!")
                        Case 4
                            RaiseEvent onLoginDenied("Commmunication Problem.")
                        Case 5
                            RaiseEvent onLoginDenied("The IGR concurrency limit has been met")
                        Case 6
                            RaiseEvent onLoginDenied("The IGR time limit has been met")
                        Case 7
                            RaiseEvent onLoginDenied("General IGR authentication failure")
                        Case Else
                            RaiseEvent onLoginDenied("Login Denied: Unknown Reason")
                    End Select
                Case Else
                    'Handle unknown packet?!?
                    '...
                    'na, just ignore it....

            End Select


        End If

        '--Are we stil conncted?
        If _LoginClient.Connected = False Then
            'Do something about being disconnected?

        Else
            '--Yes, then resize bytes to packet size
            ReDim bytes(PacketSize - 1)

            '--Call BeginReceive again, catching any error
            Try
                _LoginStream.BeginRead(bytes, 0, bytes.Length, AddressOf LoginRecieve, bytes)
            Catch ex As Exception
                'Deal with the disconnect?
            End Try
        End If
    End Sub

    ''' <summary>Connects to the specified login server and populates the ServerList property.</summary>
    ''' <param name="Address">The address of the login server to connect to.</param>
    ''' <param name="Port">The port to connect to (default is 2593).</param>
    ''' <param name="Username">The username to connect with.</param>
    ''' <param name="Password">The cooresponding password for the supplied username.</param>
    Public Overloads Function GetServerList(ByVal Username As String, ByVal Password As String, Optional ByVal Address As String = "login.uogamers.com", Optional ByVal Port As UShort = 2593) As String
        Try
            _LoginClient = New TcpClient()
            _LoginClient.ReceiveBufferSize = PacketSize
            _LoginClient.Connect(Address, Port)

        Catch ex As Exception
            Return "ERROR: Unable to connect to login server: " & ex.Message
        End Try

        _LoginStream = _LoginClient.GetStream

        'idk wtf this is for, but it seems to need it to make the server happy.
        Dim TZPacket() As Byte = {239}
        Send(TZPacket)

        Dim LoginPacket(81) As Byte

        'Generate the encryption seed.
        Dim seed() As Byte = BitConverter.GetBytes(_EncryptionLoginSeed)

        'add the seed to the login packet.
        InsertBytes(seed, LoginPacket, 0, 0, 4)

        'Add the version number.
        LoginPacket(4) = _EmulatedVersion(0)
        LoginPacket(5) = _EmulatedVersion(1)
        LoginPacket(6) = _EmulatedVersion(2)
        LoginPacket(7) = _EmulatedVersion(3)

        LoginPacket(8) = _EmulatedVersion(4)
        LoginPacket(9) = _EmulatedVersion(5)
        LoginPacket(10) = _EmulatedVersion(6)
        LoginPacket(11) = _EmulatedVersion(7)

        LoginPacket(12) = _EmulatedVersion(8)
        LoginPacket(13) = _EmulatedVersion(9)
        LoginPacket(14) = _EmulatedVersion(10)
        LoginPacket(15) = _EmulatedVersion(11)

        LoginPacket(16) = _EmulatedVersion(12)
        LoginPacket(17) = _EmulatedVersion(13)
        LoginPacket(18) = _EmulatedVersion(14)
        LoginPacket(19) = _EmulatedVersion(15)

        'Necessary...
        LoginPacket(20) = 128

        'Add the username.
        InsertBytes(GetBytesFromString(30, Username), LoginPacket, 0, 21, 30)
        _Username = Username

        'Add the password.
        InsertBytes(GetBytesFromString(30, Password), LoginPacket, 0, 51, 30)
        _Password = Password

        'Add umm 93?
        LoginPacket(81) = 93

        'Synchronouslysend the packet to the server.
        Send(LoginPacket)
        _LoginServerAddress = DirectCast(_LoginClient.Client.RemoteEndPoint, System.Net.IPEndPoint).Address.ToString

        'Set up asynchronous reading.
        Dim RecBuffer(PacketSize) As Byte
        _LoginStream.BeginRead(RecBuffer, 0, RecBuffer.Length, AddressOf LoginRecieve, RecBuffer)

        Return "SUCCESS"

    End Function

    Public Overloads Function ChooseServer(ByRef Index As Byte) As Boolean
        'If not connected then simply give up and return false.
        If Not Connected Then Return False

        Dim SelectBytes(2) As Byte
        SelectBytes(0) = 160
        SelectBytes(1) = 0
        SelectBytes(2) = Index

        If _Encrypted Then LoginCryptBytes(SelectBytes)

        Send(SelectBytes)

        Return True
    End Function

    Public Sub ChooseCharacter(ByRef CharacterName As String, ByRef Password As String, ByVal Slot As Byte)
        If Not _GameClient.Connected Then Exit Sub

        Dim packet(72) As Byte
        '0x5D Char Login Packet
        packet(0) = 93

        'Unknown 0xEDEDEDED
        packet(1) = 237
        packet(2) = 237
        packet(3) = 237
        packet(4) = 237

        Dim CharBytes() As Byte = GetBytesFromString(30, CharacterName)
        Dim PasswordBytes() As Byte = GetBytesFromString(30, Password)

        'Character Name
        For i As Integer = 5 To 34
            packet(i) = CharBytes(i - 5)
        Next

        'Character Password
        For i As Integer = 35 To 64
            packet(i) = PasswordBytes(i - 35)
        Next

        'Character Slot
        packet(65) = 0
        packet(66) = 0
        packet(67) = 0
        packet(68) = Slot

        'Dim EncBytes() As Byte = BitConverter.GetBytes(AccountUID)
        'User's encryption key
        packet(69) = 192
        packet(70) = 168
        packet(71) = 1
        packet(72) = 120

        'MsgBox(BitConverter.ToString(packet))
        Send(packet)

    End Sub

#Region "Huffman Decompression Code"

#Region "Huffman Tree"

    Private HuffmanDecompressingTree(,) As Short = _
    {{2, 1},
     {4, 3},
     {0, 5},
     {7, 6},
     {9, 8},
     {11, 10},
     {13, 12},
     {14, -256},
     {16, 15},
     {18, 17},
     {20, 19},
     {22, 21},
     {23, -1},
     {25, 24},
     {27, 26},
     {29, 28},
     {31, 30},
     {33, 32},
     {35, 34},
     {37, 36},
     {39, 38},
     {-64, 40},
     {42, 41},
     {44, 43},
     {45, -6},
     {47, 46},
     {49, 48},
     {51, 50},
     {52, -119},
     {53, -32},
     {-14, 54},
     {-5, 55},
     {57, 56},
     {59, 58},
     {-2, 60},
     {62, 61},
     {64, 63},
     {66, 65},
     {68, 67},
     {70, 69},
     {72, 71},
     {73, -51},
     {75, 74},
     {77, 76},
     {-111, -101},
     {-97, -4},
     {79, 78},
     {80, -110},
     {-116, 81},
     {83, 82},
     {-255, 84},
     {86, 85},
     {88, 87},
     {90, 89},
     {-10, -15},
     {92, 91},
     {93, -21},
     {94, -117},
     {96, 95},
     {98, 97},
     {100, 99},
     {101, -114},
     {102, -105},
     {103, -26},
     {105, 104},
     {107, 106},
     {109, 108},
     {111, 110},
     {-3, 112},
     {-7, 113},
     {-131, 114},
     {-144, 115},
     {117, 116},
     {118, -20},
     {120, 119},
     {122, 121},
     {124, 123},
     {126, 125},
     {128, 127},
     {-100, 129},
     {-8, 130},
     {132, 131},
     {134, 133},
     {135, -120},
     {-31, 136},
     {138, 137},
     {-234, -109},
     {140, 139},
     {142, 141},
     {144, 143},
     {145, -112},
     {146, -19},
     {148, 147},
     {-66, 149},
     {-145, 150},
     {-65, -13},
     {152, 151},
     {154, 153},
     {155, -30},
     {157, 156},
     {158, -99},
     {160, 159},
     {162, 161},
     {163, -23},
     {164, -29},
     {165, -11},
     {-115, 166},
     {168, 167},
     {170, 169},
     {171, -16},
     {172, -34},
     {-132, 173},
     {-108, 174},
     {-22, 175},
     {-9, 176},
     {-84, 177},
     {-37, -17},
     {178, -28},
     {180, 179},
     {182, 181},
     {184, 183},
     {186, 185},
     {-104, 187},
     {-78, 188},
     {-61, 189},
     {-178, -79},
     {-134, -59},
     {-25, 190},
     {-18, -83},
     {-57, 191},
     {192, -67},
     {193, -98},
     {-68, -12},
     {195, 194},
     {-128, -55},
     {-50, -24},
     {196, -70},
     {-33, -94},
     {-129, 197},
     {198, -74},
     {199, -82},
     {-87, -56},
     {200, -44},
     {201, -248},
     {-81, -163},
     {-123, -52},
     {-113, 202},
     {-41, -48},
     {-40, -122},
     {-90, 203},
     {204, -54},
     {-192, -86},
     {206, 205},
     {-130, 207},
     {208, -53},
     {-45, -133},
     {210, 209},
     {-91, 211},
     {213, 212},
     {-88, -106},
     {215, 214},
     {217, 216},
     {-49, 218},
     {220, 219},
     {222, 221},
     {224, 223},
     {226, 225},
     {-102, 227},
     {228, -160},
     {229, -46},
     {230, -127},
     {231, -103},
     {233, 232},
     {234, -60},
     {-76, 235},
     {-121, 236},
     {-73, 237},
     {238, -149},
     {-107, 239},
     {240, -35},
     {-27, -71},
     {241, -69},
     {-77, -89},
     {-118, -62},
     {-85, -75},
     {-58, -72},
     {-80, -63},
     {-42, 242},
     {-157, -150},
     {-236, -139},
     {-243, -126},
     {-214, -142},
     {-206, -138},
     {-146, -240},
     {-147, -204},
     {-201, -152},
     {-207, -227},
     {-209, -154},
     {-254, -153},
     {-156, -176},
     {-210, -165},
     {-185, -172},
     {-170, -195},
     {-211, -232},
     {-239, -219},
     {-177, -200},
     {-212, -175},
     {-143, -244},
     {-171, -246},
     {-221, -203},
     {-181, -202},
     {-250, -173},
     {-164, -184},
     {-218, -193},
     {-220, -199},
     {-249, -190},
     {-217, -230},
     {-216, -169},
     {-197, -191},
     {243, -47},
     {245, 244},
     {247, 246},
     {-159, -148},
     {249, 248},
     {-93, -92},
     {-225, -96},
     {-95, -151},
     {251, 250},
     {252, -241},
     {-36, -161},
     {254, 253},
     {-39, -135},
     {-124, -187},
     {-251, 255},
     {-238, -162},
     {-38, -242},
     {-125, -43},
     {-253, -215},
     {-208, -140},
     {-235, -137},
     {-237, -158},
     {-205, -136},
     {-141, -155},
     {-229, -228},
     {-168, -213},
     {-194, -224},
     {-226, -196},
     {-233, -183},
     {-167, -231},
     {-189, -174},
     {-166, -252},
     {-222, -198},
     {-179, -188},
     {-182, -223},
     {-186, -180},
     {-247, -245}}

#End Region

    Private Function DecompressHuffman(ByVal buffer As Byte()) As Byte()
        Dim MyNode As Short = 0
        Dim DecomBytes As New MemoryStream(PacketSize * 2)
        Dim OldPos As UInteger = 0

        For x As Integer = 0 To buffer.Length - 1

            For y As Integer = 7 To 0 Step -1
                MyNode = HuffmanDecompressingTree(MyNode, buffer(x) >> y And 1)

                Select Case MyNode
                    Case -256
                        MyNode = 0
                        Exit For
                    Case Is < 1
                        DecomBytes.WriteByte(CByte((-MyNode)))
                        MyNode = 0
                        Exit Select
                End Select

            Next

        Next

        Return DecomBytes.ToArray
    End Function

    Public Function DecompressSinglePacket(ByRef Peek As Boolean) As Byte()
        Dim MyNode As Short = 0
        Dim DecomBytes As New MemoryStream(PacketSize * 2)
        Dim OldPos As UInteger = 0

        For x As Integer = 0 To GameBuffer.Size - 1

            For y As Integer = 7 To 0 Step -1
                MyNode = HuffmanDecompressingTree(MyNode, GameBuffer.Peek(x) >> y And 1)

                Select Case MyNode
                    Case -256
                        MyNode = 0
                        If Not Peek Then GameBuffer.AdvanceTail(x + 1)

                        If _Encrypted Then
                            'Read the encrypted packet into a byte array
                            Dim packet() As Byte = DecomBytes.ToArray

                            'Pass the array to the decrypter to be decrypted
                            GameCryptBytes(packet)

                            'Return the newly decrypted packet.
                            Return packet
                        End If

                        Return DecomBytes.ToArray
                    Case Is < 1
                        DecomBytes.WriteByte(CByte((-MyNode)))
                        MyNode = 0
                        Exit Select
                End Select

            Next

        Next

        'Return pure endless emptyness...
        Return Nothing
    End Function

#End Region

End Class
