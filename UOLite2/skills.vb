'Implements skills use and skill change tracking.

Imports System.IO

Partial Class LiteClient

    Private _Skills(60) As SupportClasses.Skill

    Public ReadOnly Property Skills As SupportClasses.Skill()
        Get
            Return _Skills
        End Get
    End Property

    Public Event onSkillUpdate(ByRef Client As LiteClient, ByRef OldSkill As SupportClasses.Skill, ByRef NewSkill As SupportClasses.Skill)

    Public Sub HandleSkillPacket(ByRef Packet As Packets.Skills)

        Select Case Packet.ListType
            Case Packets.Skills.ListTypes.BasicWithSkillCap
                _Skills = Packet.Skills

            Case Packets.Skills.ListTypes.SkillUpdate
                Dim OldSkill As New SupportClasses.Skill(Me, _Skills(Packet.SingleSkill._Skill + 1)._Skill, _Skills(Packet.SingleSkill._Skill + 1)._Lock)
                OldSkill._BaseValue = _Skills(Packet.SingleSkill._Skill + 1)._BaseValue
                OldSkill._Value = _Skills(Packet.SingleSkill._Skill + 1)._Value
                OldSkill._Cap = _Skills(Packet.SingleSkill._Skill + 1)._Cap

                _Skills(Packet.SingleSkill._Skill + 1)._BaseValue = Packet.SingleSkill._BaseValue
                _Skills(Packet.SingleSkill._Skill + 1)._Lock = Packet.SingleSkill._Lock
                _Skills(Packet.SingleSkill._Skill + 1)._Value = Packet.SingleSkill._Value

                RaiseEvent onSkillUpdate(Me, OldSkill, Packet.SingleSkill)

            Case Packets.Skills.ListTypes.SkillUpdateWithSkillCap
                Dim OldSkill As New SupportClasses.Skill(Me, _Skills(Packet.SingleSkill._Skill + 1)._Skill, _Skills(Packet.SingleSkill._Skill + 1)._Lock)
                OldSkill._BaseValue = _Skills(Packet.SingleSkill._Skill + 1)._BaseValue
                OldSkill._Value = _Skills(Packet.SingleSkill._Skill + 1)._Value
                OldSkill._Cap = _Skills(Packet.SingleSkill._Skill + 1)._Cap

                _Skills(Packet.SingleSkill._Skill + 1)._BaseValue = Packet.SingleSkill._BaseValue
                _Skills(Packet.SingleSkill._Skill + 1)._Lock = Packet.SingleSkill._Lock
                _Skills(Packet.SingleSkill._Skill + 1)._Value = Packet.SingleSkill._Value
                _Skills(Packet.SingleSkill._Skill + 1)._Cap = Packet.SingleSkill._Cap

                RaiseEvent onSkillUpdate(Me, OldSkill, Packet.SingleSkill)
        End Select

    End Sub

    Public Sub RequestSkills()
        Dim packet As New MemoryStream
        packet.WriteByte(&H34)
        packet.WriteByte(&HED)
        packet.WriteByte(&HED)
        packet.WriteByte(&HED)
        packet.WriteByte(&HED)
        packet.WriteByte(&H5)
        packet.Write(Player.Serial.GetBytes, 0, 4)

        Send(packet.ToArray)
    End Sub

End Class

Namespace SupportClasses

    Public Class Skill
        Friend _Skill As Enums.Skills
        Friend _Lock As Enums.SkillLock
        Friend _Value As UShort
        Friend _BaseValue As UShort
        Friend _Client As LiteClient
        Friend _Cap As UShort = 1000

        Friend Sub New(ByRef Client As LiteClient, ByRef Skill As Enums.Skills, Optional ByRef Lock As Enums.SkillLock = &H0)
            _Client = Client
            _Skill = Skill
        End Sub

        ''' <summary>Gets or sets the skill's lock status.</summary>
        Public Property Lock As Enums.SkillLock
            Get
                Return _Lock
            End Get
            Set(ByVal value As Enums.SkillLock)
                If value <> _Lock Then
                    'Set the lock localy in memory.
                    _Lock = value

                    'Build a packet.
                    Dim packet As New MemoryStream
                    packet.WriteByte(&H3A)
                    packet.WriteByte(0)
                    packet.WriteByte(6)
                    packet.WriteByte(0)
                    packet.WriteByte(_Skill - 1) 'Zero based skill #
                    packet.WriteByte(value)

                    'Send the packet to update lock status.
                    _Client.Send(packet.ToArray)
                End If

            End Set
        End Property

        ''' <summary>
        ''' The skill's value.
        ''' </summary>
        Public ReadOnly Property Value As UShort
            Get
                Return _Value
            End Get
        End Property

        ''' <summary>
        ''' What you see when you click "show real".
        ''' </summary>
        Public ReadOnly Property BaseValue As UShort
            Get
                Return _BaseValue
            End Get
        End Property

        ''' <summary>
        ''' The cap of the skill.
        ''' </summary>
        Public ReadOnly Property Cap As UShort
            Get
                Return _Cap
            End Get
        End Property

        ''' <summary>
        ''' Attempt to use the skill, if the skill isn't a usable skill, nothing will happen.
        ''' </summary>
        Public Sub Use()
            Dim str As String = ""
            Dim usable As Boolean = False

            Select Case _Skill - 1
                Case 1
                    str = "1 0" & Chr(0)
                    usable = True
                Case 2
                    str = "2 0" & Chr(0)
                    usable = True
                Case 3
                    str = "3 0" & Chr(0)
                    usable = True
                Case 4
                    str = "4 0" & Chr(0)
                    usable = True
                Case 6
                    str = "6 0" & Chr(0)
                    usable = True
                Case 9
                    str = "9 0" & Chr(0)
                    usable = True
                Case 12
                    str = "12 0" & Chr(0)
                    usable = True
                Case 14
                    str = "14 0" & Chr(0)
                    usable = True
                Case 15
                    str = "15 0" & Chr(0)
                    usable = True
                Case 16
                    str = "16 0" & Chr(0)
                    usable = True
                Case 19
                    str = "19 0" & Chr(0)
                    usable = True
                Case 21
                    str = "21 0" & Chr(0)
                    usable = True
                Case 22
                    str = "22 0" & Chr(0)
                    usable = True
                Case 23
                    str = "23 0" & Chr(0)
                    usable = True
                Case 30
                    str = "30 0" & Chr(0)
                    usable = True
                Case 32
                    str = "32 0" & Chr(0)
                    usable = True
                Case 33
                    str = "33 0" & Chr(0)
                    usable = True
                Case 35
                    str = "35 0" & Chr(0)
                    usable = True
                Case 36
                    str = "36 0" & Chr(0)
                    usable = True
                Case 38
                    str = "38 0" & Chr(0)
                    usable = True
            End Select

            If usable Then
                Dim packet As New MemoryStream
                Dim strbytes() As Byte = _Client.GetBytesFromString(str.Length, str, True)

                packet.WriteByte(&H12)
                packet.WriteByte(0)
                packet.WriteByte(4 + str.Length)
                packet.WriteByte(&H24)
                packet.Write(strbytes, 0, strbytes.Length)

                _Client.Send(packet.ToArray)
            End If

        End Sub

        Public ReadOnly Property Name As String
            Get
                Return DirectCast(_Skill + 1, Enums.Skills).ToString
            End Get
        End Property

    End Class

End Namespace

'Skills Enumeration
Namespace Enums
    Public Enum SkillLock
        Up
        Down
        Locked
    End Enum

    Public Enum Virtues
        Honor
        Sacrifice
        Valor
    End Enum
End Namespace
