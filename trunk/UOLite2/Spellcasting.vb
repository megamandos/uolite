Imports System.IO

Partial Class LiteClient

    Public Sub CastSpell(ByVal Spell As UOLite2.Enums.Spell)
        Dim packet As New MemoryStream

        Dim spl As String = Spell

        packet.WriteByte(&H12)
        packet.WriteByte(0)
        packet.WriteByte(4 + spl.Length)
        packet.WriteByte(&H56)
        packet.Write(GetBytesFromString(spl.Length, spl, True), 0, spl.Length)

        Send(packet.ToArray)
    End Sub

End Class