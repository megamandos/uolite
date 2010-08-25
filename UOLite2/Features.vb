Partial Class LiteClient

    Private _Features As SupportClasses.Features

    Public ReadOnly Property Features As SupportClasses.Features
        Get
            Return _Features
        End Get
    End Property

End Class

Namespace SupportClasses

    Public Class Features
        Private _Features As UInt32

        Friend Sub New(ByRef FeaturesInt As UInt32)
            _Features = FeaturesInt
        End Sub

        Public ReadOnly Property T2A As Boolean
            Get
                Return (_Features And Enums.Features.enableT2Afeatures_chatbutton_regions)
            End Get
        End Property

        Public ReadOnly Property Renaissance As Boolean
            Get
                Return (_Features And Enums.Features.enableRenaissanceFeatures)
            End Get
        End Property

        Public ReadOnly Property ThirdDawn As Boolean
            Get
                Return (_Features And Enums.Features.enableThirdDawnFeatures)
            End Get
        End Property

        Public ReadOnly Property LBR As Boolean
            Get
                Return (_Features And Enums.Features.enableLBRfeatures_skills_map)
            End Get
        End Property

        Public ReadOnly Property AOS As Boolean
            Get
                Return (_Features And Enums.Features.enableAOSfeatures_skills_spells_map_fightbook)
            End Get
        End Property

        Public ReadOnly Property SixthCharacterSlot As Boolean
            Get
                Return (_Features And Enums.Features.enable6thcharacterslot)
            End Get
        End Property

        Public ReadOnly Property SE As Boolean
            Get
                Return (_Features And Enums.Features.enableSEfeatures_spells_skills_map)
            End Get
        End Property

        Public ReadOnly Property ML As Boolean
            Get
                Return (_Features And Enums.Features.enableMLfeatures_elvenrace_spells_skills)
            End Get
        End Property

        Public ReadOnly Property EighthAgeSplashScreen As Boolean
            Get
                Return (_Features And Enums.Features.enableTheEightAgesplashscreen)
            End Get
        End Property

        Public ReadOnly Property NinthAgeSplashScreen As Boolean
            Get
                Return (_Features And Enums.Features.enableTheNinthAgesplashscreen)
            End Get
        End Property

        Public ReadOnly Property SeventhCharacterSlot As Boolean
            Get
                Return (_Features And Enums.Features.enable7thcharacterslot)
            End Get
        End Property

        Public ReadOnly Property KRFaces As Boolean
            Get
                Return (_Features And Enums.Features.enableTheTenthAgeKRfaces)
            End Get
        End Property

        Public ReadOnly Property TrialAccount As Boolean
            Get
                Return (_Features And Enums.Features.enableTrialAccount)
            End Get
        End Property

        Public ReadOnly Property EleventhAge As Boolean
            Get
                Return (_Features And Enums.Features.enable11thAge)
            End Get
        End Property

        Public ReadOnly Property SA As Boolean
            Get
                Return (_Features And Enums.Features.enableSA)
            End Get
        End Property

    End Class

End Namespace

Namespace Enums
    <Flags()> _
    Public Enum Features As UInteger
        enableT2Afeatures_chatbutton_regions = &H1
        enableRenaissanceFeatures = &H2
        enableThirdDawnFeatures = &H4
        enableLBRfeatures_skills_map = &H8
        enableAOSfeatures_skills_spells_map_fightbook = &H10
        enable6thcharacterslot = &H20
        enableSEfeatures_spells_skills_map = &H40
        enableMLfeatures_elvenrace_spells_skills = &H80
        enableTheEightAgesplashscreen = &H100
        enableTheNinthAgesplashscreen = &H200
        enable7thcharacterslot = &H1000
        enableTheTenthAgeKRfaces = &H2000
        enableTrialAccount = &H4000
        enable11thAge = &H8000
        enableSA = &H10000
    End Enum
End Namespace

Namespace Packets
    Public Class Features
        Inherits Packet

        Private _Features As SupportClasses.Features

        Friend Sub New(ByRef Bytes() As Byte)
            MyBase.New(Enums.PacketType.Features)

            buff = New UOLite2.SupportClasses.BufferHandler(Bytes, True)

            With buff
                .Position = 1
                _Features = New SupportClasses.Features(.readuint)
            End With

        End Sub

        Public ReadOnly Property Features As SupportClasses.Features
            Get
                Return _Features
            End Get
        End Property

    End Class
End Namespace