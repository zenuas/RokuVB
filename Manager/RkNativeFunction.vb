Imports System
Imports Roku.Operator
Imports Roku.IntermediateCode


Namespace Manager

    Public Class RkNativeFunction
        Inherits RkFunction

        Public Overridable Property [Operator] As InOperator = InOperator.Nop

        Public Overrides Function CreateCall(self As OpValue, ParamArray args() As OpValue) As InCode0()

            Select Case Me.Operator
                Case InOperator.Return

                    If args.Length > 0 Then

                        Return {New InCode With {.Operator = InOperator.Return, .Left = args(0)}}
                    Else
                        Return {New InCode0 With {.Operator = InOperator.Return}}
                    End If

                Case InOperator.Nop

                    Return New InCode0() {}

                Case Else
                    Throw New NotSupportedException

            End Select
        End Function

        Public Overrides Function CreateCallReturn(self As OpValue, return_ As OpValue, ParamArray args() As OpValue) As InCode0()

            If args.Length <> Me.Arguments.Count Then Throw New ArgumentException("argument count")
            If Me.Operator = InOperator.Nop Then Return New InCode0() {}

            Dim x As New InCode With {.Operator = Me.Operator, .Left = If(args.Length > 0, args(0), Nothing), .Right = If(args.Length > 1, args(1), Nothing), .Return = return_}
            Return New InCode0() {x}
        End Function

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkNativeFunction With {.Name = Me.Name, .Operator = Me.Operator, .Scope = Me.Scope, .GenericBase = Me}
            x.Scope.AddFunction(x)
            Return x
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} '{Me.Operator}'"
        End Function

    End Class

End Namespace
