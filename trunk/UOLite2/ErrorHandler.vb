'To handle unhandled exceptions in the UOLite code.

Partial Class LiteClient

    Private Sub MYExnHandler(ByVal sender As Object,
                             ByVal e As UnhandledExceptionEventArgs)

        Dim EX As Exception
        EX = e.ExceptionObject
        'Console.WriteLine(EX.StackTrace)
        MsgBox("There has been an unhandled exception, please go to this site http://code.google.com/p/uolite/issues/entry report this: " & vbNewLine & vbNewLine & "ERROR:" & EX.Message & vbNewLine & vbNewLine & "Stack Trace: " & EX.StackTrace, MsgBoxStyle.Critical, "UNHANDLED EXCEPTION IN UOLITE2")

    End Sub

    Private Sub MYThreadHandler(ByVal sender As Object,
                                ByVal e As Threading.ThreadExceptionEventArgs)

        Console.WriteLine(e.Exception.StackTrace)

    End Sub

End Class