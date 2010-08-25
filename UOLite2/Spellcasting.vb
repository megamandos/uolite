Imports System.IO

Partial Class LiteClient

    Public Sub CastSpell(ByVal Spell As UOLite2.Enums.Spell)
        Dim packet As New MemoryStream

        Dim spl As String = Spell
        spl = spl & Chr(0)

        packet.WriteByte(&H12)
        packet.WriteByte(0)
        packet.WriteByte(5)
        packet.WriteByte(&H56)
        packet.WriteByte(spl)

        Send(packet.ToArray)
    End Sub

End Class