'Used to handle new mobile packets, and provides a class to track mobiles in the game.

#Const DebugMobileList = False

Partial Class LiteClient

    'Hide this class from the user, there is no reason from him/her to see it.
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
    Public Class MobileList
        Implements IEnumerable
        Private _MobileHashBySerial As New Hashtable
        Private _Client As LiteClient
        Private _IsSearchResult As Boolean = False

        Friend Event AddedMobile(ByVal Mobile As Mobile)

        Friend Sub New(ByRef Client As LiteClient)
            _Client = Client
        End Sub

        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Adds the specified mobile to the mobile list.
        ''' </summary>
        Public Sub AddMobile(ByVal Mobile As Mobile)

            _MobileHashBySerial.Add(Mobile.Serial, Mobile)
            'TODO: Make an AddHash sub for post-packet handling to add this to the memory offset hash.
            '_MobileHashByOffset.Add(Mobile.MemoryOffset, Mobile

#If DebugMobileList Then
            Console.WriteLine("-Added Mobile: " & Mobile.Serial.ToString)
#End If
            RaiseEvent AddedMobile(Mobile)
        End Sub

        ''' <summary>
        ''' Adds the a new mobile to the <see cref="MobileList"/> based on a supplied packet.
        ''' </summary>
        ''' <param name="Packet">The packet as a <see cref="Packets.EquippedMobile"/>.</param>
        Friend Sub AddMobile(ByVal Packet As Packets.EquippedMobile)
            If Exists(Packet.Serial) Then
                'Instead of trying to create a new mobile, just tell the mobile to udpate itself with the given packet.
                Me.Mobile(Packet.Serial).HandleUpdatePacket(Packet)

                'Return to the sub that called this.
                Exit Sub
            End If

            'Create a new empty mobile on the current client.
            Dim NewMobile As New Mobile(_Client, Packet.Serial)

            NewMobile._Type = Packet.BodyType
            NewMobile._Amount = Packet.Amount
            NewMobile._X = Packet.X
            NewMobile._Y = Packet.Y
            NewMobile._Z = Packet.Z
            NewMobile._Direction = Packet.Direction
            NewMobile._Hue = Packet.Hue
            NewMobile._Notoriety = Packet.Notoriety
            NewMobile._Status = Packet.Status

#If DebugMobileList Then
            Console.WriteLine("-Adding Mobile as Equipped Mobile:" & NewMobile.Serial.ToString)
#End If

            'Make the mobile
            AddMobile(NewMobile)

            'Have the mobile dress itself.
            NewMobile.HandleUpdatePacket(Packet)

        End Sub

        ''' <param name="Packet">The packet as a <see cref="Packets.NakedMobile"/>.</param>
        Friend Sub AddMobile(ByVal Packet As Packets.NakedMobile)
            If Exists(Packet.Serial) Then
                'Instead of trying to create a new mobile, just tell the mobile to udpate itself with the given packet.
                Me.Mobile(Packet.Serial).HandleUpdatePacket(Packet)

                'Return to the sub that called this.
                Exit Sub
            End If

            'Create a new empty mobile on the current client.
            Dim NewMobile As New Mobile(_Client, Packet.Serial)

            NewMobile._Type = Packet.BodyType
            NewMobile._X = Packet.X
            NewMobile._Y = Packet.Y
            NewMobile._Z = Packet.Z
            NewMobile._Direction = Packet.Direction
            NewMobile._Hue = Packet.Hue
            NewMobile._Notoriety = Packet.Notoriety
            NewMobile._Status = Packet.Status
#If DebugMobileList Then
            Console.WriteLine("-Adding Mobile as Naked Mobile:" & NewMobile.Serial.ToString)
#End If
            AddMobile(NewMobile)
        End Sub

        ''' <summary>
        ''' Removes the specified mobile from the MobileList.
        ''' </summary>
        ''' <param name="Serial">The serial of the mobile to be removed.</param>
        Public Function RemoveMobile(ByVal Serial As Serial)
            Try
                If Exists(Serial) Then
                    _MobileHashBySerial.Remove(Serial)
                Else
                    Throw New ApplicationException("The mobile does not exist in the hash!")
                End If
            Catch ex As Exception

#If DebugMobileList Then
                Console.WriteLine("-Remove Mobile by Serial Failed: " & Serial.ToString)
                Console.WriteLine(" Failure Reason: " & ex.Message)
#End If

                Return False
            End Try

#If DebugMobileList Then
            Console.WriteLine("-Removed Mobile by Serial: " & Serial.ToString)
#End If
            Return True
        End Function

        Friend Sub RemoveMobile(ByVal DeathPacket As Packets.DeathAnimation)
            DirectCast(_MobileHashBySerial(DeathPacket.Serial), Mobile).HandleDeathPacket(DeathPacket)
            RemoveMobile(DeathPacket.Serial)
#If DebugMobileList Then
            Console.WriteLine("-Removed Mobile by Death Packet: " & DeathPacket.Serial.ToString)
#End If
        End Sub

        ''' <summary>
        ''' Checks to see if the specified mobile exists in the MobileList.
        ''' </summary>
        ''' <param name="Serial">The serial of the mobile to check for.</param>
        Public Function Exists(ByVal Serial As Serial) As Boolean
            Return _MobileHashBySerial.Contains(Serial)
        End Function

        ''' <summary>
        ''' Returns a MobileList containing all of the mobiles with the specified name.
        ''' </summary>
        ''' <param name="Name">The name you want to search for.</param>
        Public Function byName(ByVal Name As String) As MobileList
            Dim ml As New MobileList

            For Each s As Serial In _MobileHashBySerial.Keys
                If DirectCast(_MobileHashBySerial(s), Mobile).Name = Name Then ml.AddMobile(DirectCast(_MobileHashBySerial(s), Mobile))
            Next

            Return ml
        End Function

        ''' <summary>
        ''' Returns mobile specified by serial.
        ''' </summary>
        ''' <param name="Serial">The serial of the mobile.</param>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property Mobile(ByVal Serial As Serial) As Mobile
            Get
                Return _MobileHashBySerial(Serial)
            End Get
        End Property

        Public ReadOnly Property Count As UInteger
            Get
                Return _MobileHashBySerial.Count
            End Get
        End Property

        Public Function byRazorSerial(ByRef Serial As String) As Serial
            If _MobileHashBySerial.ContainsKey(_Client.GetSerialFromString(Serial)) Then
                Return _Client.GetSerialFromString(Serial)
            Else
                Return ZeroSerial
            End If
        End Function

        Public Function GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return _MobileHashBySerial.Values.GetEnumerator()
        End Function
    End Class

End Class
