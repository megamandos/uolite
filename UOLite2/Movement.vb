'For player movement. Like walking/running/follow mobiles and items/ whatever...

'Displays debug messages for movement.
#Const DebugMovement = False

Partial Public Class LiteClient

#Region "Walking/Running"

    Protected Friend StepSync As Byte = 0
    Private FastWalkEnabled As Boolean = False
    Private Shared _FastWalkKeys As New Stack(Of UInteger)
    Private MovementInterval As UShort = 200 'ms between sending movement request packets.
    Private MovementBuffer As New SupportClasses.CircularBuffer(Of MoveRequest)(1000)
    Private WithEvents MoveTicker As New System.Timers.Timer(MovementInterval) With {.Enabled = False}
    Private _MovementPaused As Boolean = False

    ''' <summary>When the player's position is updated, this is called.</summary>
    Public Event onPlayerMove(ByRef Client As LiteClient)

    ''' <summary>
    ''' Called when the player tries to walk and is prevented for whatever reason.
    ''' </summary>
    Public Event onMovementBlocked(ByRef Client As LiteClient)

    ''' <summary>
    ''' Pauses player movement and prevents adding movements to the movement queue when set to false.
    ''' </summary>
    Public Property MovementPaused As Boolean
        Get
            Return _MovementPaused
        End Get
        Set(ByVal value As Boolean)
            If value = True And _MovementPaused = False Then
                _MovementPaused = value
                MoveTicker.Enabled = False
            ElseIf value = False And _MovementPaused = True Then
                _MovementPaused = value
                MoveTicker.Enabled = True
            End If
        End Set
    End Property

    Private Structure MoveRequest
        Public Direction As UOLite2.Enums.Direction
        Public Follow As Boolean
    End Structure

    Public Sub Walk(ByRef Direction As UOLite2.Enums.Direction, Optional ByRef NumberOfSteps As UInteger = 1)
        'Stop following someone if you are, since this will take you off track anyways.
        StopFollowing()
        TakeStep(Direction, NumberOfSteps, False)
    End Sub

    Private Sub TakeStep(ByRef Direction As UOLite2.Enums.Direction, Optional ByRef NumberOfSteps As UInteger = 1, Optional ByRef Follow As Boolean = False)
        If Not _MovementPaused Then
            Dim x As MoveRequest

            'Write the movement request to the move buffer the specified number of times.
            For i As Integer = 0 To NumberOfSteps - 1
                x = New MoveRequest With {.Direction = Direction,
                                          .Follow = Follow}
                MovementBuffer.WriteSingle(x)
            Next

            'Enable the moveticker/ensure its enabled.
            MoveTicker.Enabled = True
        End If
    End Sub

    Private Sub MoveTicker_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles MoveTicker.Elapsed
        If MovementBuffer.Size = 0 Then
            MoveTicker.Enabled = False
#If DebugMovement Then
            Debug.WriteLine("MOVEMENT: Buffer empty, halting movement.")
#End If
            Exit Sub
        End If

        Dim x As MoveRequest = MovementBuffer.ReadSingle

        'if not following someone then skip the follow related command.
        If x.Follow = True And _Following = False Then Exit Sub

        Dim key() As Byte

        If FastWalkEnabled Then
            'pop a key off the stack
            key = BitConverter.GetBytes(_FastWalkKeys.Pop)
        Else
            'Set key to 1
            key = {0, 0, 0, 0}
        End If

        Dim packet(6) As Byte
        packet(0) = 2
        packet(1) = x.Direction
        packet(2) = StepSync
        packet(3) = key(0)
        packet(4) = key(1)
        packet(5) = key(2)
        packet(6) = key(3)

        'Send the packet.
        Send(packet)

        'Increment stepsync
        If StepSync = 255 Then
            StepSync = 1
        Else
            StepSync += 1
        End If

        'Remove running from the direction.
        If x.Direction > &H80 Then x.Direction -= &H80

        'Check if the player is facing that direction or not.
        If Not Player.Direction = x.Direction Then
            'If he isnt, then turn that direction.
            Player._Direction = x.Direction
        Else
            'if he is, then take a step.
            'Adjust the player's position, assuming that it will accept your movement request.
            Select Case x.Direction
                Case UOLite2.Enums.Direction.North
                    Player._Y -= 1

                Case UOLite2.Enums.Direction.NorthEast
                    Player._Y -= 1
                    Player._X += 1

                Case UOLite2.Enums.Direction.East
                    Player._X += 1

                Case UOLite2.Enums.Direction.SouthEast
                    Player._X += 1
                    Player._Y += 1

                Case UOLite2.Enums.Direction.South
                    Player._Y += 1

                Case UOLite2.Enums.Direction.SouthWest
                    Player._Y += 1
                    Player._X -= 1

                Case UOLite2.Enums.Direction.West
                    Player._X -= 1

                Case UOLite2.Enums.Direction.NorthWest
                    Player._X -= 1
                    Player._Y -= 1

            End Select

        End If

#If DebugMovement Then
        Debug.WriteLine("MOVEMENT: Sent Walk " & x.Direction.ToString)
#End If

        HandlePlayerMovement()

    End Sub

    Private Sub MovementBlocked(ByRef packet As Packets.BlockMovement)
        'Stop trying to move.
        MoveTicker.Enabled = False

        'reset the syncstep to 0
        StepSync = 0

        'Empty the movement buffer.
        MovementBuffer.Clear()

        'set the player position and direction.
        Player._X = packet.X
        Player._Y = packet.Y
        Player._Z = packet.Z
        Player._Direction = packet.Direction

#If DebugMovement Then
        Debug.WriteLine("MOVEMENT: Recieved Movement Blocked")
#End If

        HandlePlayerMovement()

        'Let the owner know the movement was blocked.
        RaiseEvent onMovementBlocked(Me)
    End Sub

    Private Sub EnableFastWalk(ByRef Packet As Packets.FastWalk)
        FastWalkEnabled = True

        'Push each key onto the stack.
        For Each u As UInteger In Packet.Keys
            AddFastWalkKey(u)
        Next

#If DebugMovement Then
        Debug.WriteLine("MOVEMENT: Received Enable Fast Walk")
#End If

    End Sub

    Private Sub AddFastWalkKey(ByRef Packet As Packets.AddWalkKey)
        'Ensure fastwalk enabled is set to true.
        FastWalkEnabled = True

        'Push the key onto the stack
        AddFastWalkKey(Packet.Key)

#If DebugMovement Then
        Debug.WriteLine("MOVEMENT: Recieved Fast Walk Key")
#End If

    End Sub

    Private Sub AddFastWalkKey(ByRef Key As UInteger)
        'Push the key onto the stack.
        _FastWalkKeys.Push(Key)
    End Sub

    Private Sub HandleTeleport(ByRef Packet As Packets.Teleport)
        'Apply the info to the mobile that was teleported, this SHOULD only be the player...
        _Mobiles.Mobile(Packet.Serial)._X = Packet.X
        _Mobiles.Mobile(Packet.Serial)._Y = Packet.Y
        _Mobiles.Mobile(Packet.Serial)._Z = Packet.Z
        _Mobiles.Mobile(Packet.Serial)._Type = Packet.Artwork
        _Mobiles.Mobile(Packet.Serial)._Direction = Packet.Direction
        _Mobiles.Mobile(Packet.Serial)._Status = Packet.Status

        If Packet.Serial = Player.Serial Then
            HandlePlayerMovement()
        End If

#If DebugMovement Then
        Debug.WriteLine("MOVEMENT: Recieved Teleport")
#End If

    End Sub

    Private Sub AcceptMovement(ByRef Packet As Packets.AcceptMovement_ResyncRequest)
        Player._Notoriety = Packet.Reputation
        HandlePlayerMovement()
    End Sub

    Private Sub HandlePlayerMovement()

        RaiseEvent onPlayerMove(Me)

        If _Following Then
            AddFollowStep()
        End If

        'Check surroundings for scavengable items.
        Scavenger.CheckSurroundings()
    End Sub

#End Region

#Region "Following"
    Private _Following As Boolean = False
    Private WithEvents FollowTarget As Mobile
    'Private WithEvents FollowTicker As New System.Timers.Timer(MovementInterval + 10) With {.Enabled = False}
    Private _MinFollowDistance As UShort = 2
    Private _MaxFollowDistance As UShort = 25

    Private Sub FollowTarget_onMove(ByRef Client As LiteClient, ByRef Mobile As Mobile) Handles FollowTarget.onMove
        If _Following Then
            AddFollowStep()
        End If
    End Sub

    Private Sub AddFollowStep()
        Dim dist As UShort = Get2DDistance(Player, FollowTarget)
        If dist > _MinFollowDistance + 1 And dist < _MaxFollowDistance - 1 Then
#If DebugMovement Then
            Debug.WriteLine("FOLLOWING: Direction: " & GetDirection(Player, FollowTarget).ToString & " Distance: " & dist)
#End If
            'Take a step toward the target.
            TakeStep(GetDirection(Player, FollowTarget), 1, True)
        Else
            'Empty the movement buffer if you are too close or too far way.
            MovementBuffer.Clear()
        End If
    End Sub

    ''' <summary>Follows the target.</summary>
    ''' <param name="Target">The serial of the target to follow.</param>
    Public Sub Follow(ByRef Target As Serial, Optional ByRef ClosestToGet As Byte = 2, Optional ByRef DistanceToStop As Byte = 25)
        If _Mobiles.Exists(Target) Then
#If DebugMovement Then
            Debug.WriteLine("FOLLOWING: Starting to follow " & _Mobiles.Mobile(Target).Name)
#End If
            _MinFollowDistance = ClosestToGet
            _MaxFollowDistance = DistanceToStop
            _Following = True
            FollowTarget = Mobiles.Mobile(Target)
        End If
    End Sub

    Public Sub StopFollowing()
        _Following = False
#If DebugMovement Then
        Debug.WriteLine("FOLLOWING: Stopped.")
#End If
    End Sub

#End Region

End Class
