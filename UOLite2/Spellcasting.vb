Imports System.IO

Partial Class LiteClient

    Public Sub CastSpell(ByRef Spell As UOLite2.Enums.Spell)
        Dim packet As New MemoryStream
        Spell += 1

        packet.WriteByte(&H12)
        packet.WriteByte(0)
        packet.WriteByte(5)
        packet.WriteByte(&H56)
        packet.WriteByte(Spell.ToString(0))

        Send(packet.ToArray)
    End Sub

End Class