
'#define		FEISTEL	 

Imports System.Diagnostics
Imports System.Security.Cryptography

'
' * This implementation of Twofish has been published here:
' * http://www.codetools.com/csharp/twofish_csharp.asp
' 


Namespace Scripts.Engines.Encryption

    ''' <summary>
    ''' Summary description for TwofishBase.
    ''' </summary>
    Friend Class TwofishBase
        Public Enum EncryptionDirection
            Encrypting
            Decrypting
        End Enum

        Public Sub New()
        End Sub

        Protected inputBlockSize As Integer = BLOCK_SIZE \ 8
        Protected outputBlockSize As Integer = BLOCK_SIZE \ 8

        '
        '		+*****************************************************************************
        '		*
        '		* Function Name:	f32
        '		*
        '		* Function:			Run four bytes through keyed S-boxes and apply MDS matrix
        '		*
        '		* Arguments:		x			=	input to f function
        '		*					k32			=	pointer to key dwords
        '		*					keyLen		=	total key length (k32 --> keyLey/2 bits)
        '		*
        '		* Return:			The output of the keyed permutation applied to x.
        '		*
        '		* Notes:
        '		*	This function is a keyed 32-bit permutation.  It is the major building
        '		*	block for the Twofish round function, including the four keyed 8x8 
        '		*	permutations and the 4x4 MDS matrix multiply.  This function is used
        '		*	both for generating round subkeys and within the round function on the
        '		*	block being encrypted.  
        '		*
        '		*	This version is fairly slow and pedagogical, although a smartcard would
        '		*	probably perform the operation exactly this way in firmware.   For
        '		*	ultimate performance, the entire operation can be completed with four
        '		*	lookups into four 256x32-bit tables, with three dword xors.
        '		*
        '		*	The MDS matrix is defined in TABLE.H.  To multiply by Mij, just use the
        '		*	macro Mij(x).
        '		*
        '		-***************************************************************************

        Private Shared Function f32(ByVal x As UInteger, ByRef k32 As UInteger(), ByVal keyLen As Integer) As UInteger
            Dim b As Byte() = {b0(x), b1(x), b2(x), b3(x)}

            ' Run each byte thru 8x8 S-boxes, xoring with key byte at each stage. 

            ' Note that each byte goes through a different combination of S-boxes.


            '*((DWORD *)b) = Bswap(x);	/* make b[0] = LSB, b[3] = MSB */
            Select Case ((keyLen + 63) \ 64) And 3
                Case 0
                    ' 256 bits of key 
                    b(0) = CByte(P8x8(P_04, b(0)) Xor b0(k32(3)))
                    b(1) = CByte(P8x8(P_14, b(1)) Xor b1(k32(3)))
                    b(2) = CByte(P8x8(P_24, b(2)) Xor b2(k32(3)))
                    b(3) = CByte(P8x8(P_34, b(3)) Xor b3(k32(3)))
                    ' fall thru, having pre-processed b[0]..b[3] with k32[3] 

                    ' 192 bits of key 
                    b(0) = CByte(P8x8(P_03, b(0)) Xor b0(k32(2)))
                    b(1) = CByte(P8x8(P_13, b(1)) Xor b1(k32(2)))
                    b(2) = CByte(P8x8(P_23, b(2)) Xor b2(k32(2)))
                    b(3) = CByte(P8x8(P_33, b(3)) Xor b3(k32(2)))
                    ' fall thru, having pre-processed b[0]..b[3] with k32[2] 

                    ' 128 bits of key 
                    b(0) = P8x8(P_00, P8x8(P_01, P8x8(P_02, b(0)) Xor b0(k32(1))) Xor b0(k32(0)))
                    b(1) = P8x8(P_10, P8x8(P_11, P8x8(P_12, b(1)) Xor b1(k32(1))) Xor b1(k32(0)))
                    b(2) = P8x8(P_20, P8x8(P_21, P8x8(P_22, b(2)) Xor b2(k32(1))) Xor b2(k32(0)))
                    b(3) = P8x8(P_30, P8x8(P_31, P8x8(P_32, b(3)) Xor b3(k32(1))) Xor b3(k32(0)))
                    Exit Select
                Case 3
                    ' 192 bits of key 
                    b(0) = CByte(P8x8(P_03, b(0)) Xor b0(k32(2)))
                    b(1) = CByte(P8x8(P_13, b(1)) Xor b1(k32(2)))
                    b(2) = CByte(P8x8(P_23, b(2)) Xor b2(k32(2)))
                    b(3) = CByte(P8x8(P_33, b(3)) Xor b3(k32(2)))
                    ' fall thru, having pre-processed b[0]..b[3] with k32[2] 

                    ' 128 bits of key 
                    b(0) = P8x8(P_00, P8x8(P_01, P8x8(P_02, b(0)) Xor b0(k32(1))) Xor b0(k32(0)))
                    b(1) = P8x8(P_10, P8x8(P_11, P8x8(P_12, b(1)) Xor b1(k32(1))) Xor b1(k32(0)))
                    b(2) = P8x8(P_20, P8x8(P_21, P8x8(P_22, b(2)) Xor b2(k32(1))) Xor b2(k32(0)))
                    b(3) = P8x8(P_30, P8x8(P_31, P8x8(P_32, b(3)) Xor b3(k32(1))) Xor b3(k32(0)))
                    Exit Select
                Case 2
                    ' 128 bits of key 
                    b(0) = P8x8(P_00, P8x8(P_01, P8x8(P_02, b(0)) Xor b0(k32(1))) Xor b0(k32(0)))
                    b(1) = P8x8(P_10, P8x8(P_11, P8x8(P_12, b(1)) Xor b1(k32(1))) Xor b1(k32(0)))
                    b(2) = P8x8(P_20, P8x8(P_21, P8x8(P_22, b(2)) Xor b2(k32(1))) Xor b2(k32(0)))
                    b(3) = P8x8(P_30, P8x8(P_31, P8x8(P_32, b(3)) Xor b3(k32(1))) Xor b3(k32(0)))
                    Exit Select
            End Select


            ' Now perform the MDS matrix multiply inline. 

            Return CUInt((M00(b(0)) Xor M01(b(1)) Xor M02(b(2)) Xor M03(b(3)))) Xor CUInt((M10(b(0)) Xor M11(b(1)) Xor M12(b(2)) Xor M13(b(3))) << 8) Xor CUInt((M20(b(0)) Xor M21(b(1)) Xor M22(b(2)) Xor M23(b(3))) << 16) Xor CUInt((M30(b(0)) Xor M31(b(1)) Xor M32(b(2)) Xor M33(b(3))) << 24)
        End Function

        '
        '		+*****************************************************************************
        '		*
        '		* Function Name:	reKey
        '		*
        '		* Function:			Initialize the Twofish key schedule from key32
        '		*
        '		* Arguments:		key			=	ptr to keyInstance to be initialized
        '		*
        '		* Return:			TRUE on success
        '		*
        '		* Notes:
        '		*	Here we precompute all the round subkeys, although that is not actually
        '		*	required.  For example, on a smartcard, the round subkeys can 
        '		*	be generated on-the-fly	using f32()
        '		*
        '		-***************************************************************************

        Protected Function reKey(ByVal keyLen As Integer, ByRef key32 As UInteger()) As Boolean
            Dim i As Integer, k64Cnt As Integer
            keyLength = keyLen
            rounds = numRounds((keyLen - 1) \ 64)
            Dim subkeyCnt As Integer = ROUND_SUBKEYS + 2 * rounds
            Dim A As UInteger, B As UInteger
            Dim k32e As UInteger() = New UInteger(MAX_KEY_BITS \ 64 - 1) {}
            Dim k32o As UInteger() = New UInteger(MAX_KEY_BITS \ 64 - 1) {}
            ' even/odd key dwords 

            k64Cnt = (keyLen + 63) \ 64
            ' round up to next multiple of 64 bits 
            For i = 0 To k64Cnt - 1
                ' split into even/odd key dwords 
                k32e(i) = key32(2 * i)
                k32o(i) = key32(2 * i + 1)
                ' compute S-box keys using (12,8) Reed-Solomon code over GF(256) 

                ' reverse order 
                sboxKeys(k64Cnt - 1 - i) = RS_MDS_Encode(k32e(i), k32o(i))
            Next

            For i = 0 To subkeyCnt \ 2 - 1
                ' compute round subkeys for PHT 
                A = f32(CUInt(i * SK_STEP), k32e, keyLen)
                ' A uses even key dwords 
                B = f32(CUInt(i * SK_STEP + SK_BUMP), k32o, keyLen)
                ' B uses odd  key dwords 
                B = ROL(B, 8)
                subKeys(2 * i) = A + B
                ' combine with a PHT 
                subKeys(2 * i + 1) = ROL(A + 2 * B, SK_ROTL)
            Next

            Return True
        End Function

        Public Sub blockDecrypt(ByRef x As UInteger())
            Dim t0 As UInteger, t1 As UInteger
            Dim xtemp As UInteger() = New UInteger(3) {}

            If cipherMode = CipherMode.CBC Then
                x.CopyTo(xtemp, 0)
            End If

            For i As Integer = 0 To BLOCK_SIZE \ 32 - 1
                ' copy in the block, add whitening 
                x(i) = x(i) Xor subKeys(OUTPUT_WHITEN + i)
            Next

            For r As Integer = rounds - 1 To 0 Step -1
                ' main Twofish decryption loop 
                t0 = f32(x(0), sboxKeys, keyLength)
                t1 = f32(ROL(x(1), 8), sboxKeys, keyLength)

                x(2) = ROL(x(2), 1)
                x(2) = x(2) Xor t0 + t1 + subKeys(ROUND_SUBKEYS + 2 * r)
                ' PHT, round keys 
                x(3) = x(3) Xor t0 + 2 * t1 + subKeys(ROUND_SUBKEYS + 2 * r + 1)
                x(3) = ROR(x(3), 1)

                If r > 0 Then
                    ' unswap, except for last round 
                    t0 = x(0)
                    x(0) = x(2)
                    x(2) = t0
                    t1 = x(1)
                    x(1) = x(3)
                    x(3) = t1
                End If
            Next

            For i As Integer = 0 To BLOCK_SIZE \ 32 - 1
                ' copy out, with whitening 
                x(i) = x(i) Xor subKeys(INPUT_WHITEN + i)
                If cipherMode = CipherMode.CBC Then
                    x(i) = x(i) Xor IV(i)
                    IV(i) = xtemp(i)
                End If
            Next
        End Sub

        Public Sub blockEncrypt(ByRef x As UInteger())
            Dim t0 As UInteger, t1 As UInteger, tmp As UInteger

            For i As Integer = 0 To BLOCK_SIZE \ 32 - 1
                ' copy in the block, add whitening 
                x(i) = x(i) Xor subKeys(INPUT_WHITEN + i)
                If cipherMode = CipherMode.CBC Then
                    x(i) = x(i) Xor IV(i)
                End If
            Next

            For r As Integer = 0 To rounds - 1
                ' main Twofish encryption loop 
                ' 16==rounds
#If FEISTEL Then
				t0 = f32(ROR(x(0), (r + 1) \ 2), sboxKeys, keyLength)
				t1 = f32(ROL(x(1), 8 + (r + 1) \ 2), sboxKeys, keyLength)
				' PHT, round keys 

				x(2) = x(2) Xor ROL(t0 + t1 + subKeys(ROUND_SUBKEYS + 2 * r), r \ 2)
				x(3) = x(3) Xor ROR(t0 + 2 * t1 + subKeys(ROUND_SUBKEYS + 2 * r + 1), (r + 2) \ 2)

#Else
                t0 = f32(x(0), sboxKeys, keyLength)
                t1 = f32(ROL(x(1), 8), sboxKeys, keyLength)

                x(3) = ROL(x(3), 1)
                x(2) = x(2) Xor t0 + t1 + subKeys(ROUND_SUBKEYS + 2 * r)
                ' PHT, round keys 
                x(3) = x(3) Xor t0 + 2 * t1 + subKeys(ROUND_SUBKEYS + 2 * r + 1)
                x(2) = ROR(x(2), 1)

#End If
                If r < rounds - 1 Then
                    ' swap for next round 
                    tmp = x(0)
                    x(0) = x(2)
                    x(2) = tmp
                    tmp = x(1)
                    x(1) = x(3)
                    x(3) = tmp
                End If
            Next
#If FEISTEL Then
			x(0) = ROR(x(0), 8)
			' "final permutation" 
			x(1) = ROL(x(1), 8)
			x(2) = ROR(x(2), 8)
			x(3) = ROL(x(3), 8)
#End If
            For i As Integer = 0 To BLOCK_SIZE \ 32 - 1
                ' copy out, with whitening 
                x(i) = x(i) Xor subKeys(OUTPUT_WHITEN + i)
                If cipherMode = CipherMode.CBC Then
                    IV(i) = x(i)
                End If
            Next

        End Sub

        Private numRounds As Integer() = {0, ROUNDS_128, ROUNDS_192, ROUNDS_256}

        '
        '		+*****************************************************************************
        '		*
        '		* Function Name:	RS_MDS_Encode
        '		*
        '		* Function:			Use (12,8) Reed-Solomon code over GF(256) to produce
        '		*					a key S-box dword from two key material dwords.
        '		*
        '		* Arguments:		k0	=	1st dword
        '		*					k1	=	2nd dword
        '		*
        '		* Return:			Remainder polynomial generated using RS code
        '		*
        '		* Notes:
        '		*	Since this computation is done only once per reKey per 64 bits of key,
        '		*	the performance impact of this routine is imperceptible. The RS code
        '		*	chosen has "simple" coefficients to allow smartcard/hardware implementation
        '		*	without lookup tables.
        '		*
        '		-***************************************************************************

        'TODO: WTF?!?!?!
        Private Shared Function RS_MDS_Encode(ByVal k0 As UInteger, ByVal k1 As UInteger) As UInteger
            Dim i As UInteger, j As UInteger
            Dim r As UInteger

            For i = InlineAssignHelper(r, 0) To 1
                r = r Xor If((i > 0), k0, k1)
                ' merge in 32 more key bits 
                For j = 0 To 3
                    ' shift one byte at a time 
                    RS_rem(r)
                Next
            Next
            Return r
        End Function

        Protected sboxKeys As UInteger() = New UInteger(MAX_KEY_BITS \ 64 - 1) {}
        ' key bits used for S-boxes 
        Protected subKeys As UInteger() = New UInteger(TOTAL_SUBKEYS - 1) {}
        ' round subkeys, input/output whitening bits 
        Protected Key As UInteger() = {0, 0, 0, 0, 0, 0, _
         0, 0}
        'new int[MAX_KEY_BITS/32];
        Protected IV As UInteger() = {0, 0, 0, 0}
        ' this should be one block size
        Private keyLength As Integer
        Private rounds As Integer
        Protected cipherMode As CipherMode = CipherMode.ECB


#Region "These are all the definitions that were found in AES.H"
        Private Shared ReadOnly BLOCK_SIZE As Integer = 128
        ' number of bits per block 
        Private Shared ReadOnly MAX_ROUNDS As Integer = 16
        ' max # rounds (for allocating subkey array) 
        Private Shared ReadOnly ROUNDS_128 As Integer = 16
        ' default number of rounds for 128-bit keys
        Private Shared ReadOnly ROUNDS_192 As Integer = 16
        ' default number of rounds for 192-bit keys
        Private Shared ReadOnly ROUNDS_256 As Integer = 16
        ' default number of rounds for 256-bit keys
        Private Shared ReadOnly MAX_KEY_BITS As Integer = 256
        ' max number of bits of key 
        Private Shared ReadOnly MIN_KEY_BITS As Integer = 128
        ' min number of bits of key (zero pad) 

        '#define		VALID_SIG	 0x48534946	/* initialization signature ('FISH') */
        '#define		MCT_OUTER			400	/* MCT outer loop */
        '#define		MCT_INNER		  10000	/* MCT inner loop */
        '#define		REENTRANT			  1	/* nonzero forces reentrant code (slightly slower) */

        Private Shared ReadOnly INPUT_WHITEN As Integer = 0
        ' subkey array indices 
        Private Shared ReadOnly OUTPUT_WHITEN As Integer = (INPUT_WHITEN + BLOCK_SIZE \ 32)
        Private Shared ReadOnly ROUND_SUBKEYS As Integer = (OUTPUT_WHITEN + BLOCK_SIZE \ 32)
        ' use 2 * (# rounds) 
        Private Shared ReadOnly TOTAL_SUBKEYS As Integer = (ROUND_SUBKEYS + 2 * MAX_ROUNDS)


#End Region

#Region "These are all the definitions that were found in TABLE.H that we need"
        ' for computing subkeys 

        Private Shared ReadOnly SK_STEP As UInteger = &H2020202UI
        Private Shared ReadOnly SK_BUMP As UInteger = &H1010101UI
        Private Shared ReadOnly SK_ROTL As Integer = 9

        ' Reed-Solomon code parameters: (12,8) reversible code
        '		g(x) = x**4 + (a + 1/a) x**3 + a x**2 + (a + 1/a) x + 1
        '		where a = primitive root of field generator 0x14D 

        Private Shared ReadOnly RS_GF_FDBK As UInteger = &H14D
        ' field generator 
        Private Shared Sub RS_rem(ByRef x As UInteger)
            Dim b As Byte = CByte(x >> 24)
            ' TODO: maybe change g2 and g3 to bytes			 
            Dim g2 As UInteger = CUInt(((b << 1) Xor (If(((b And &H80) = &H80), RS_GF_FDBK, 0))) And &HFF)
            Dim g3 As UInteger = CUInt(((b >> 1) And &H7F) Xor (If(((b And 1) = 1), RS_GF_FDBK >> 1, 0)) Xor g2)
            x = (x << 8) Xor (g3 << 24) Xor (g2 << 16) Xor (g3 << 8) Xor b
        End Sub

        '	Macros for the MDS matrix
        '		*	The MDS matrix is (using primitive polynomial 169):
        '		*      01  EF  5B  5B
        '		*      5B  EF  EF  01
        '		*      EF  5B  01  EF
        '		*      EF  01  EF  5B
        '		*----------------------------------------------------------------
        '		* More statistical properties of this matrix (from MDS.EXE output):
        '		*
        '		* Min Hamming weight (one byte difference) =  8. Max=26.  Total =  1020.
        '		* Prob[8]:      7    23    42    20    52    95    88    94   121   128    91
        '		*             102    76    41    24     8     4     1     3     0     0     0
        '		* Runs[8]:      2     4     5     6     7     8     9    11
        '		* MSBs[8]:      1     4    15     8    18    38    40    43
        '		* HW= 8: 05040705 0A080E0A 14101C14 28203828 50407050 01499101 A080E0A0 
        '		* HW= 9: 04050707 080A0E0E 10141C1C 20283838 40507070 80A0E0E0 C6432020 07070504 
        '		*        0E0E0A08 1C1C1410 38382820 70705040 E0E0A080 202043C6 05070407 0A0E080E 
        '		*        141C101C 28382038 50704070 A0E080E0 4320C620 02924B02 089A4508 
        '		* Min Hamming weight (two byte difference) =  3. Max=28.  Total = 390150.
        '		* Prob[3]:      7    18    55   149   270   914  2185  5761 11363 20719 32079
        '		*           43492 51612 53851 52098 42015 31117 20854 11538  6223  2492  1033
        '		* MDS OK, ROR:   6+  7+  8+  9+ 10+ 11+ 12+ 13+ 14+ 15+ 16+
        '		*               17+ 18+ 19+ 20+ 21+ 22+ 23+ 24+ 25+ 26+
        '		

        Private Shared ReadOnly MDS_GF_FDBK As Integer = &H169
        ' primitive polynomial for GF(256)
        Private Shared Function LFSR1(ByVal x As Integer) As Integer
            Return (((x) >> 1) Xor (If((((x) And &H1) = &H1), MDS_GF_FDBK \ 2, 0)))
        End Function

        Private Shared Function LFSR2(ByVal x As Integer) As Integer
            Return (((x) >> 2) Xor (If((((x) And &H2) = &H2), MDS_GF_FDBK \ 2, 0)) Xor (If((((x) And &H1) = &H1), MDS_GF_FDBK \ 4, 0)))
        End Function

        ' TODO: not the most efficient use of code but it allows us to update the code a lot quicker we can possibly optimize this code once we have got it all working
        Private Shared Function Mx_1(ByVal x As Integer) As Integer
            Return x
            ' force result to int so << will work 
        End Function

        Private Shared Function Mx_X(ByVal x As Integer) As Integer
            Return x Xor LFSR2(x)
            ' 5B 
        End Function

        Private Shared Function Mx_Y(ByVal x As Integer) As Integer
            Return x Xor LFSR1(x) Xor LFSR2(x)
            ' EF 
        End Function

        Private Shared Function M00(ByVal x As Integer) As Integer
            Return Mul_1(x)
        End Function
        Private Shared Function M01(ByVal x As Integer) As Integer
            Return Mul_Y(x)
        End Function
        Private Shared Function M02(ByVal x As Integer) As Integer
            Return Mul_X(x)
        End Function
        Private Shared Function M03(ByVal x As Integer) As Integer
            Return Mul_X(x)
        End Function

        Private Shared Function M10(ByVal x As Integer) As Integer
            Return Mul_X(x)
        End Function
        Private Shared Function M11(ByVal x As Integer) As Integer
            Return Mul_Y(x)
        End Function
        Private Shared Function M12(ByVal x As Integer) As Integer
            Return Mul_Y(x)
        End Function
        Private Shared Function M13(ByVal x As Integer) As Integer
            Return Mul_1(x)
        End Function

        Private Shared Function M20(ByVal x As Integer) As Integer
            Return Mul_Y(x)
        End Function
        Private Shared Function M21(ByVal x As Integer) As Integer
            Return Mul_X(x)
        End Function
        Private Shared Function M22(ByVal x As Integer) As Integer
            Return Mul_1(x)
        End Function
        Private Shared Function M23(ByVal x As Integer) As Integer
            Return Mul_Y(x)
        End Function

        Private Shared Function M30(ByVal x As Integer) As Integer
            Return Mul_Y(x)
        End Function
        Private Shared Function M31(ByVal x As Integer) As Integer
            Return Mul_1(x)
        End Function
        Private Shared Function M32(ByVal x As Integer) As Integer
            Return Mul_Y(x)
        End Function
        Private Shared Function M33(ByVal x As Integer) As Integer
            Return Mul_X(x)
        End Function

        Private Shared Function Mul_1(ByVal x As Integer) As Integer
            Return Mx_1(x)
        End Function
        Private Shared Function Mul_X(ByVal x As Integer) As Integer
            Return Mx_X(x)
        End Function
        Private Shared Function Mul_Y(ByVal x As Integer) As Integer
            Return Mx_Y(x)
        End Function
        '	Define the fixed p0/p1 permutations used in keyed S-box lookup.  
        '			By changing the following constant definitions for P_ij, the S-boxes will
        '			automatically get changed in all the Twofish source code. Note that P_i0 is
        '			the "outermost" 8x8 permutation applied.  See the f32() function to see
        '			how these constants are to be  used.
        '		

        Private Shared ReadOnly P_00 As Integer = 1
        ' "outermost" permutation 
        Private Shared ReadOnly P_01 As Integer = 0
        Private Shared ReadOnly P_02 As Integer = 0
        Private Shared ReadOnly P_03 As Integer = (P_01 Xor 1)
        ' "extend" to larger key sizes 
        Private Shared ReadOnly P_04 As Integer = 1

        Private Shared ReadOnly P_10 As Integer = 0
        Private Shared ReadOnly P_11 As Integer = 0
        Private Shared ReadOnly P_12 As Integer = 1
        Private Shared ReadOnly P_13 As Integer = (P_11 Xor 1)
        Private Shared ReadOnly P_14 As Integer = 0

        Private Shared ReadOnly P_20 As Integer = 1
        Private Shared ReadOnly P_21 As Integer = 1
        Private Shared ReadOnly P_22 As Integer = 0
        Private Shared ReadOnly P_23 As Integer = (P_21 Xor 1)
        Private Shared ReadOnly P_24 As Integer = 0

        Private Shared ReadOnly P_30 As Integer = 0
        Private Shared ReadOnly P_31 As Integer = 1
        Private Shared ReadOnly P_32 As Integer = 1
        Private Shared ReadOnly P_33 As Integer = (P_31 Xor 1)
        Private Shared ReadOnly P_34 As Integer = 1

        ' fixed 8x8 permutation S-boxes 


        '**********************************************************************
        '		*  07:07:14  05/30/98  [4x4]  TestCnt=256. keySize=128. CRC=4BD14D9E.
        '		* maxKeyed:  dpMax = 18. lpMax =100. fixPt =  8. skXor =  0. skDup =  6. 
        '		* log2(dpMax[ 6..18])=   --- 15.42  1.33  0.89  4.05  7.98 12.05
        '		* log2(lpMax[ 7..12])=  9.32  1.01  1.16  4.23  8.02 12.45
        '		* log2(fixPt[ 0.. 8])=  1.44  1.44  2.44  4.06  6.01  8.21 11.07 14.09 17.00
        '		* log2(skXor[ 0.. 0])
        '		* log2(skDup[ 0.. 6])=   ---  2.37  0.44  3.94  8.36 13.04 17.99
        '		**********************************************************************

        '  p0:   

        '  dpMax      = 10.  lpMax      = 64.  cycleCnt=   1  1  1  0.         

        ' 817D6F320B59ECA4.ECB81235F4A6709D.BA5E6D90C8F32471.D7F4126E9B3085CA. 

        ' Karnaugh maps:
        '			*  0111 0001 0011 1010. 0001 1001 1100 1111. 1001 1110 0011 1110. 1101 0101 1111 1001. 
        '			*  0101 1111 1100 0100. 1011 0101 0010 0000. 0101 1000 1100 0101. 1000 0111 0011 0010. 
        '			*  0000 1001 1110 1101. 1011 1000 1010 0011. 0011 1001 0101 0000. 0100 0010 0101 1011. 
        '			*  0111 0100 0001 0110. 1000 1011 1110 1001. 0011 0011 1001 1101. 1101 0101 0000 1100. 
        '			

        '  p1:   

        '  dpMax      = 10.  lpMax      = 64.  cycleCnt=   2  0  0  1.         

        ' 28BDF76E31940AC5.1E2B4C376DA5F908.4C75169A0ED82B3F.B951C3DE647F208A. 

        ' Karnaugh maps:
        '			*  0011 1001 0010 0111. 1010 0111 0100 0110. 0011 0001 1111 0100. 1111 1000 0001 1100. 
        '			*  1100 1111 1111 1010. 0011 0011 1110 0100. 1001 0110 0100 0011. 0101 0110 1011 1011. 
        '			*  0010 0100 0011 0101. 1100 1000 1000 1110. 0111 1111 0010 0110. 0000 1010 0000 0011. 
        '			*  1101 1000 0010 0001. 0110 1001 1110 0101. 0001 0100 0101 0111. 0011 1011 1111 0010. 
        '			

        Private Shared P8x8 As Byte(,) = {{&HA9, &H67, &HB3, &HE8, &H4, &HFD, _
         &HA3, &H76, &H9A, &H92, &H80, &H78, _
         &HE4, &HDD, &HD1, &H38, &HD, &HC6, _
         &H35, &H98, &H18, &HF7, &HEC, &H6C, _
         &H43, &H75, &H37, &H26, &HFA, &H13, _
         &H94, &H48, &HF2, &HD0, &H8B, &H30, _
         &H84, &H54, &HDF, &H23, &H19, &H5B, _
         &H3D, &H59, &HF3, &HAE, &HA2, &H82, _
         &H63, &H1, &H83, &H2E, &HD9, &H51, _
         &H9B, &H7C, &HA6, &HEB, &HA5, &HBE, _
         &H16, &HC, &HE3, &H61, &HC0, &H8C, _
         &H3A, &HF5, &H73, &H2C, &H25, &HB, _
         &HBB, &H4E, &H89, &H6B, &H53, &H6A, _
         &HB4, &HF1, &HE1, &HE6, &HBD, &H45, _
         &HE2, &HF4, &HB6, &H66, &HCC, &H95, _
         &H3, &H56, &HD4, &H1C, &H1E, &HD7, _
         &HFB, &HC3, &H8E, &HB5, &HE9, &HCF, _
         &HBF, &HBA, &HEA, &H77, &H39, &HAF, _
         &H33, &HC9, &H62, &H71, &H81, &H79, _
         &H9, &HAD, &H24, &HCD, &HF9, &HD8, _
         &HE5, &HC5, &HB9, &H4D, &H44, &H8, _
         &H86, &HE7, &HA1, &H1D, &HAA, &HED, _
         &H6, &H70, &HB2, &HD2, &H41, &H7B, _
         &HA0, &H11, &H31, &HC2, &H27, &H90, _
         &H20, &HF6, &H60, &HFF, &H96, &H5C, _
         &HB1, &HAB, &H9E, &H9C, &H52, &H1B, _
         &H5F, &H93, &HA, &HEF, &H91, &H85, _
         &H49, &HEE, &H2D, &H4F, &H8F, &H3B, _
         &H47, &H87, &H6D, &H46, &HD6, &H3E, _
         &H69, &H64, &H2A, &HCE, &HCB, &H2F, _
         &HFC, &H97, &H5, &H7A, &HAC, &H7F, _
         &HD5, &H1A, &H4B, &HE, &HA7, &H5A, _
         &H28, &H14, &H3F, &H29, &H88, &H3C, _
         &H4C, &H2, &HB8, &HDA, &HB0, &H17, _
         &H55, &H1F, &H8A, &H7D, &H57, &HC7, _
         &H8D, &H74, &HB7, &HC4, &H9F, &H72, _
         &H7E, &H15, &H22, &H12, &H58, &H7, _
         &H99, &H34, &H6E, &H50, &HDE, &H68, _
         &H65, &HBC, &HDB, &HF8, &HC8, &HA8, _
         &H2B, &H40, &HDC, &HFE, &H32, &HA4, _
         &HCA, &H10, &H21, &HF0, &HD3, &H5D, _
         &HF, &H0, &H6F, &H9D, &H36, &H42, _
         &H4A, &H5E, &HC1, &HE0}, {&H75, &HF3, &HC6, &HF4, &HDB, &H7B, _
         &HFB, &HC8, &H4A, &HD3, &HE6, &H6B, _
         &H45, &H7D, &HE8, &H4B, &HD6, &H32, _
         &HD8, &HFD, &H37, &H71, &HF1, &HE1, _
         &H30, &HF, &HF8, &H1B, &H87, &HFA, _
         &H6, &H3F, &H5E, &HBA, &HAE, &H5B, _
         &H8A, &H0, &HBC, &H9D, &H6D, &HC1, _
         &HB1, &HE, &H80, &H5D, &HD2, &HD5, _
         &HA0, &H84, &H7, &H14, &HB5, &H90, _
         &H2C, &HA3, &HB2, &H73, &H4C, &H54, _
         &H92, &H74, &H36, &H51, &H38, &HB0, _
         &HBD, &H5A, &HFC, &H60, &H62, &H96, _
         &H6C, &H42, &HF7, &H10, &H7C, &H28, _
         &H27, &H8C, &H13, &H95, &H9C, &HC7, _
         &H24, &H46, &H3B, &H70, &HCA, &HE3, _
         &H85, &HCB, &H11, &HD0, &H93, &HB8, _
         &HA6, &H83, &H20, &HFF, &H9F, &H77, _
         &HC3, &HCC, &H3, &H6F, &H8, &HBF, _
         &H40, &HE7, &H2B, &HE2, &H79, &HC, _
         &HAA, &H82, &H41, &H3A, &HEA, &HB9, _
         &HE4, &H9A, &HA4, &H97, &H7E, &HDA, _
         &H7A, &H17, &H66, &H94, &HA1, &H1D, _
         &H3D, &HF0, &HDE, &HB3, &HB, &H72, _
         &HA7, &H1C, &HEF, &HD1, &H53, &H3E, _
         &H8F, &H33, &H26, &H5F, &HEC, &H76, _
         &H2A, &H49, &H81, &H88, &HEE, &H21, _
         &HC4, &H1A, &HEB, &HD9, &HC5, &H39, _
         &H99, &HCD, &HAD, &H31, &H8B, &H1, _
         &H18, &H23, &HDD, &H1F, &H4E, &H2D, _
         &HF9, &H48, &H4F, &HF2, &H65, &H8E, _
         &H78, &H5C, &H58, &H19, &H8D, &HE5, _
         &H98, &H57, &H67, &H7F, &H5, &H64, _
         &HAF, &H63, &HB6, &HFE, &HF5, &HB7, _
         &H3C, &HA5, &HCE, &HE9, &H68, &H44, _
         &HE0, &H4D, &H43, &H69, &H29, &H2E, _
         &HAC, &H15, &H59, &HA8, &HA, &H9E, _
         &H6E, &H47, &HDF, &H34, &H35, &H6A, _
         &HCF, &HDC, &H22, &HC9, &HC0, &H9B, _
         &H89, &HD4, &HED, &HAB, &H12, &HA2, _
         &HD, &H52, &HBB, &H2, &H2F, &HA9, _
         &HD7, &H61, &H1E, &HB4, &H50, &H4, _
         &HF6, &HC2, &H16, &H25, &H86, &H56, _
         &H55, &H9, &HBE, &H91}}
#End Region

#Region "These are all the definitions that were found in PLATFORM.H that we need"
        ' left rotation
        Private Shared Function ROL(ByVal x As UInteger, ByVal n As Integer) As UInteger
            Return (((x) << ((n) And &H1F)) Or (x) >> (32 - ((n) And &H1F)))
        End Function

        ' right rotation
        Private Shared Function ROR(ByVal x As UInteger, ByVal n As Integer) As UInteger
            Return (((x) >> ((n) And &H1F)) Or ((x) << (32 - ((n) And &H1F))))
        End Function

        ' first byte
        Protected Shared Function b0(ByVal x As UInteger) As Byte
            Return CByte(x)
            '& 0xFF);
        End Function
        ' second byte
        Protected Shared Function b1(ByVal x As UInteger) As Byte
            Return CByte((x >> 8))
            ' & (0xFF));
        End Function
        ' third byte
        Protected Shared Function b2(ByVal x As UInteger) As Byte
            Return CByte((x >> 16))
            ' & (0xFF));
        End Function
        ' fourth byte
        Protected Shared Function b3(ByVal x As UInteger) As Byte
            Return CByte((x >> 24))
            ' & (0xFF));
        End Function
        Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, ByVal value As T) As T
            target = value
            Return value
        End Function

#End Region
    End Class
End Namespace