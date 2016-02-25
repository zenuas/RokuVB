Imports System


Namespace Util

    Public Class Errors

        Public Shared Sub Logging(Of E As Exception)(f As Action, log As Action(Of E))

            Try
                f()

            Catch ex As E

                log(ex)
                Throw

            End Try
        End Sub

        Public Shared Function Logging(Of R, E As Exception)(f As Func(Of R), log As Action(Of E)) As R

            Try
                Return f()

            Catch ex As E

                log(ex)
                Throw

            End Try
        End Function

        Public Shared Sub These(ParamArray fs() As Action)

            For i = 0 To fs.Length - 1

                Try
                    fs(i)()

                Catch When i < fs.Length - 1

                End Try
            Next
        End Sub

        Public Shared Function These(Of R)(ParamArray fs() As Func(Of R)) As R

            For i = 0 To fs.Length - 1

                Try
                    Return fs(i)()

                Catch When i < fs.Length - 1

                End Try
            Next
        End Function

        Public Shared Sub Retry(retry_ As Integer, f As Action)

            Do While True

                Try
                    f()
                    Return

                Catch When retry_ > 0

                    retry_ -= 1
                    Continue Do

                End Try
            Loop
        End Sub

        Public Shared Function Retry(Of R)(retry_ As Integer, f As Func(Of R)) As R

            Do While True

                Try
                    Return f()

                Catch When retry_ > 0

                    retry_ -= 1
                    Continue Do

                End Try
            Loop
        End Function

        Public Shared Function [Default](Of R)(default_ As R, ParamArray fs() As Func(Of R)) As R

            Try
                Return These(fs)

            Catch

                Return default_

            End Try
        End Function

        Public Shared Function [Default](Of R)(default_ As Func(Of R), ParamArray fs() As Func(Of R)) As R

            Try
                Return These(fs)

            Catch

                Return default_()

            End Try
        End Function

    End Class

End Namespace
