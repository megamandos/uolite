' The "ActionBufferClass" class was built to buffer doubleclicks and pickup commands,
' the intent was to aleviate some of the stress of having to handle the failed
' attempts when they are made in rapid succession.

Partial Class LiteClient
    Protected Friend ActionBuffer As SupportClasses.ActionBufferClass

    Public ReadOnly Property PlayerBusy As Boolean
        Get
            Return ActionBuffer.Busy
        End Get
    End Property

End Class

Namespace SupportClasses

    Public Class ActionBufferClass
        Private _Client As LiteClient
        Private WithEvents CommandDelayTimer As System.Timers.Timer
        Private _Interval As UShort
        Private _Latency As UShort
        Private _Busy As Boolean = False

        Friend Sub New(ByRef Client As LiteClient, Optional ByRef CommandDelay As UShort = 650)
            _Client = Client
            _Latency = Client.Latency
            _Interval = CommandDelay
            CommandDelayTimer = New System.Timers.Timer(_Interval + _Latency) With {.Enabled = False}
        End Sub

        Private ActionQueue As New CircularBuffer(Of Action)(100)

        Friend Structure Action
            Public Type As ActionType
            Public TargetSerial As Serial
            Public Amount As UShort
        End Structure

        Friend Enum ActionType As Byte
            DoubleClick
            PickupItem
        End Enum

        Friend Sub Add(ByRef Type As ActionType, ByRef Target As Serial, Optional ByRef Amount As UShort = 0)
            If ActionQueue.Size = ActionQueue.RealSize - 1 Then
                Throw New ApplicationException("Action Queue has overflowed! To avoid this, check your code where actions like pickup items and drop items will be attempted in rapid succession.")
            End If

            Dim BillyJohn As New Action With {.Type = Type, .TargetSerial = Target, .Amount = Amount}
            ActionQueue.WriteSingle(BillyJohn)

            If CommandDelayTimer.Enabled = False Then
                'Tell it to kick off immediately.
                _Busy = True
                DoAction()
                CommandDelayTimer.Enabled = True
            End If
        End Sub

        Friend ReadOnly Property Busy As Boolean
            Get
                Return _Busy
            End Get
        End Property

        Private Sub CommandDelayTimer_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles CommandDelayTimer.Elapsed
            DoAction()
        End Sub

        Private Sub DoAction()
            If ActionQueue.Size = 1 Then
                CommandDelayTimer.Enabled = False
                _Busy = False
            ElseIf ActionQueue.Size = 0 Then
                CommandDelayTimer.Enabled = False
                _Busy = False
                Exit Sub
            End If

            Dim CurrentAction As Action = ActionQueue.ReadSingle

            Select Case CurrentAction.Type
                Case ActionType.PickupItem
                    If _Client._ItemInHand.Value = 0 Then
                        _Client._ItemInHand = CurrentAction.TargetSerial
                        _Client.Send(New Packets.TakeObject(CurrentAction.TargetSerial, CurrentAction.Amount))
                    End If

                Case ActionType.DoubleClick

                    'Make the packet
                    Dim dc As New Packets.Doubleclick

                    'Assign the serial
                    dc.Serial = CurrentAction.TargetSerial

                    'Send the packet to the server.
                    _Client.Send(dc)

            End Select
        End Sub


    End Class

End Namespace