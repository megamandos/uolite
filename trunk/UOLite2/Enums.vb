Partial Class LiteClient

    ''' <summary>A simple list of enumerations used primarily internally.</summary>
    Public Class Enums
        ''' <summary>Enumeration of Login state.</summary>
        Public Enum LoginState
            AT_LOGIN_GUMP
            AT_CHARACTERLIST
            AT_SERVERLIST
            LOGIN_FAILURE
            LOGIN_SUCCESS
        End Enum

        ''' <summary>Enumeration of Entity types.</summary>
        Public Enum EntityType
            Item
            Mobile
            ItemList
            Gump
            GumpList
            GumpElement
            GumpElementList
            Journal
            JournalEntry
            PacketQueue
            Packet
        End Enum

        ''' <summary>Enumeration of Spells.</summary>
        Public Enum Spell
            ' First circle
            Clumsy = 1
            CreateFood
            Feeblemind
            Heal
            MagicArrow
            NightSight
            ReactiveArmor
            Weaken

            ' Second circle                                    
            Agility
            Cunning
            Cure
            Harm
            MagicTrap
            RemoveTrap
            Protection
            Strength

            ' Third circle                                     
            Bless
            Fireball
            MagicLock
            Poison
            Telekinesis
            Teleport
            Unlock
            WallOfStone

            ' Fourth circle                                    
            ArchCure
            ArchProtection
            Curse
            FireField
            GreaterHeal
            Lightning
            ManaDrain
            Recall

            ' Fifth circle                                     
            BladeSpirits
            DispelField
            Incognito
            MagicReflect
            MindBlast
            Paralyze
            PoisonField
            SummonCreature

            ' Sixth circle                                     
            Dispel
            EnergyBolt
            Explosion
            Invisibility
            Mark
            MassCurse
            ParalyzeField
            Reveal

            ' Seventh circle                                   
            ChainLightning
            EnergyField
            FlameStrike
            GateTravel
            ManaVampire
            MassDispel
            MeteorSwarm
            Polymorph

            ' Eighth circle                                    
            Earthquake
            EnergyVortex
            Resurrection
            AirElemental
            SummonDaemon
            EarthElemental
            FireElemental
            WaterElemental

            'Necromancy
            AnimateDead = 101
            BloodOath
            CorpseSkin
            CurseWeapon
            EvilOmen
            HorrificBeast
            LichForm
            MindRot
            PainSpike
            PoisonStrike
            Strangle
            SummonFamiliar
            VampiricEmbrace
            VengefulSpirit
            Wither
            WraithForm
            Exorcism

            'Chevalry
            CleanseByFire = 201
            CloseWounds
            ConsecrateWeapon
            DispelEvil
            DivineFury
            EnemyOfOne
            HolyLight
            NobleSacrifice
            RemoveCurse
            SacredJourney

            'TODO: add ninjitsu, bushido and spellweaving
        End Enum

        ''' <summary>Enumeration of macro types.</summary>
        Public Enum Macros
            Say = 1
            Emote
            Whisper
            Yell
            Walk
            ToggleWarMode
            Paste
            Open
            Close
            Minimize
            Maximize
            OpenDoor
            UseSkill
            LastSkill
            CastSpell
            LastSpell
            Bow
            Salute
            QuitGame
            AllNames
            LastTarget
            TargetSelf
            ArmOrDisarm
            WaitForTarget
            TargetNext
            AttackLast
            Delay
            CircleTrans
            CloseAllGumps
            AlwaysRun
            SaveDesktop
            KillGumpOpen
            UsePrimaryAbility
            UseSecondaryAbility
            EquipLastWeapon
            SetUpdateRange
            ModifyUpdateRange
            IncreaseUpdateRange
            DecreaseUpdateRange
            MaxUpdateRange
            MinUpdateRange
            DefaultUpdateRange
            UpdateRangeInfo
            EnableRangeColor
            DisableRangeColor
            InvokeVirtue
        End Enum

        ''' <summary>Enumeration of element types.</summary>
        Public Enum Element
            Configuration
            Paperdoll
            Status
            Journal
            Skills
            MageSpellbook
            Chat
            Backpack
            Overview
            Mail
            PartyManifest
            PartyChat
            NecroSpellbook
            PaladinSpellbook
            CombatBook
            BushidoSpellbook
            NinjitsuSpellbook
            Guild
            SpellWeavingSpellbook
            QuestLog
        End Enum

        <Flags()> _
        Public Enum Features As Short
            enableT2Afeatures_chatbutton_regions = &H1
            enablerenaissancefeatures = &H2
            enablethirddownfeatures = &H4
            enableLBRfeatures_skills_map = &H8
            enableAOSfeatures_skills_spells_map_fightbook = &H10
            enable6thcharacterslot = &H20
            enableSEfeatures_spells_skills_map = &H40
            enableMLfeatures_elvenrace_spells_skills = &H80
            enableTheEightAgesplashscreen = &H100
            enableTheNinthAgesplashscreen = &H200
            enable7thcharacterslot = &H1000
            enableTheTenthAgeKRfaces = &H2000
        End Enum

        ''' <summary>Enumeration of mobile gender.</summary>
        Public Enum Gender As Byte
            Male = &H0
            Female = &H1
            Neutral = &H2
        End Enum

        ''' <summary>Enumeration of mobile status.</summary>
        Public Enum MobileStatus
            Normal = &H0
            Unknown = &H1
            CanAlterPaperdoll = &H2
            Poisoned = &H4
            GoldenHealth = &H8
            Unknown2 = &H10
            Unknown3 = &H20
            WarMode = &H40
            Hidden = &H80
        End Enum

        ''' <summary>Enumeration of the different facets.</summary>
        Public Enum Facets
            Felucca
            Trammel
            Ilshenar
            Malas
            Tokuno
            Internal = &H7F
        End Enum

        Public Enum PacketOrigin
            FROMCLIENT
            FROMSERVER
            INTERNAL
        End Enum

        Public Enum PacketDestination
            SERVER
            CLIENT
        End Enum

        'Supported Clients
        Public Enum ClientVersions
            UOML
        End Enum

        'UOML Fonts
        Public Enum Fonts As UShort
            BigFont = &H0
            ShadowFont = &H1
            BigShadowFont = &H2
            [Default] = &H3
            Gothic = &H4
            Italic = &H5
            SmallAndDark = &H6
            ColorFull = &H7
            Runes = &H8
            SmallAndLight = &H9
        End Enum

        'UOML Directions
        Public Enum Direction As Byte
            North = &H0
            NorthEast = &H1
            East = &H2
            SouthEast = &H3
            South = &H4
            SouthWest = &H5
            West = &H6
            NorthWest = &H7
            NorthRunning = &H80
            NorthEastRunning = &H81
            EastRunning = &H82
            SouthEastRunning = &H83
            SouthRunning = &H84
            SouthWestRunning = &H85
            WestRunning = &H86
            NorthWestRunning = &H87

            'custom, meaning no direction, used by 'following' algorithm.
            None = &HFF

        End Enum

        Public Enum WarMode
            Disabled
            Enabled
        End Enum

        'UOML ItemFlags
        <Flags()> _
        Public Enum ItemFlags As UInteger
            Poisoned = &H4
            GoldenHealth = &H8
            WarMode = &H40
            Hidden = &H80
        End Enum

        Public Enum Reputation As UInteger
            Normal = &H0
            Innocent = &H1
            GuildMember = &H2
            Neutral = &H3
            Criminal = &H4
            Enemy = &H5
            Murderer = &H6
            Invulnerable = &H7
        End Enum

        Public Enum Layers As Byte
            None = &H0
            LeftHand = &H1
            RightHand = &H2
            Shoes = &H3
            Pants = &H4
            Shirt = &H5
            Head = &H6
            Gloves = &H7
            Ring = &H8
            Neck = &HA
            Hair = &HB
            Waist = &HC
            InnerTorso = &HD
            Bracelet = &HE
            FacialHair = &H10
            MiddleTorso = &H11
            Ears = &H12
            Arms = &H13
            Back = &H14
            BackPack = &H15
            OuterTorso = &H16
            OuterLegs = &H17
            InnerLegs = &H18
            Mount = &H19
            Bank = &H1D
        End Enum

        <Flags()> _
        Public Enum TypeFlags As UInteger
            Background = &H1
            Weapon = &H2
            Transparent = &H4
            Translucent = &H8
            Wall = &H10
            Damaging = &H20
            Impassable = &H40
            Wet = &H80
            Surface = &H200
            Bridge = &H400
            Stackable = &H800
            Window = &H1000
            NoShoot = &H2000
            PrefixA = &H4000
            PrevixAn = &H8000
            Internal = &H10000
            Foliage = &H20000
            PartiallyHued = &H40000
            Map = &H100000
            Container = &H200000
            Wearable = &H400000
            LightSource = &H800000
            Animated = &H1000000
            NoDiagonal = &H2000000
            Armor = &H8000000
            Roof = &H10000000
            Door = &H20000000
            StairBack = &H40000000
            StairRight = 4294967295
        End Enum

        Public Enum SpeechTypes
            Regular = &H0
            Broadcast = &H1
            Emote = &H2
            System = &H6
            Whisper = &H8
            Yell = &H9
        End Enum

        Public Enum EventTypes
            Any
            Packet
            DoubleClick
            SingleClick
            KeyUp
            KeyDown
            NewItem
            NewMobile
            ItemDeletion
            Drag
            Drop
            MobileUpdate
        End Enum

        Public Class Common
            Public Enum Hues As UShort
                BlueDark = 3
                Blue = 99
                BlueLight = 101

                RedDark = 32
                Red = 33
                RedLight = 35

                YellowDark = 52
                Yellow = 53
                YellowLight = 55

                GreenDark = 67
                Green = 73
                GreenLight = 70

                VioletDark = 17
                Violet = 18
                VioletLight = 21

                OrangeDark = 42
                Orange = 43
                OrangeLight = 45

                AquaDark = 82
                Aqua = 83
                AquaLight = 85
            End Enum

            Public Enum BodyTypes As UShort
                HumanMale = 400
                HumanFemale = 401
                HumanMaleDead = 402
                HumanFemaleDead = 403
            End Enum

            Public Enum ItemTypes As UShort
                GoldCoins = 3821

                Pouch = 3705
                Bag = 3702
                Backpack = 3701
                WoodenBox = 2474

                RedPotion = 3851
                YellowPotion = 3852
                PurplePotion = 3853
                EmptyBottle = 3854
                OrangePotion = 3847

                BlackPearl = 3962
                BloodMoss = 3963
                Garlic = 3972
                Ginseng = 3973
                MandrakeRoot = 3974
                Nightshade = 3976
                SpiderSilk = 3981
                SulfurousAsh = 3980

                RawRibs = 2545
                DragonScales = 9808
                Hides = 4217
                Leather = 4225

                Scissors = 3999
                Dagger = 3922
                Cutlass = 5185
                Scimitar = 5046
                SkinningKnife = 3780
                ButcherKnife = 5110
                VikingSword = 5049
                Kryss = 5121
                Cleaver = 3779
                Katana = 5119
                Broadsword = 3934
                Longsword = 3937
                Longsword2 = 5048

                RidingChimera = 16016
                RidingCuSidhe = 16017
                RidingChargeroftheFallen = 16018
                RidingHiyrur = 16020
                Ridingbeetle = 16021
                Ridingbeetle2 = 16023
                Ridingswampdragon = 16024
                RidingRidgback = 16026
                RidingUni = 16027
                RidingKiri = 16028
                RidingUni2 = 16029
                RidingFiresteed = 16030
                RidingHorse = 16031
                Ridinggreyhorse = 16032
                Ridinghorse2 = 16033
                Ridingbrownhorse = 16034
                Ridingos = 16035
                Ridingzos = 16036
                Ridingzos2 = 16037
                Ridingllama = 16038
                Ridingnightmare = 16039
                RidingSilverSteed = 16040
                Ridingnightmare2 = 16041
                Ridingethyhorse = 16042
                Ridingethyllama = 16043
                Ridingethyos = 16044
                Ridingkirin = 16045
                RidingMinaxWarhorse = 16047
                RidingShadowLordsWarhorse = 16048
                RidingCOMWarhorse = 16049
                RidingTRUEBritsWarhorse = 16050
                Ridingseahorse = 16051
                Ridinguni3 = 16052
                Ridingnightmare3 = 16053
                Ridingnightmare4 = 16054
                Ridingdarknightmare = 16055
                Ridingridgbeack = 16056
                Ridingridgeback = 16058
                Ridingundeadhorse = 16059
                Ridingbeetle3 = 16060
                Ridingswampdragon2 = 16061
                Ridingarmoredswampdragon = 16062
                RidingPolarBear = 16069
                RidingDaemon = 16239
            End Enum
        End Class


    End Class

End Class
