Imports System
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util


Namespace Manager

    Public Class RkNativeFunction
        Inherits RkFunction

        Public Overridable Property [Operator] As InOperator = InOperator.Nop

        Public Overrides Function CreateCall(ParamArray args() As OpValue) As InCode0()

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

        Public Overrides Function CreateCallReturn(return_ As OpValue, ParamArray args() As OpValue) As InCode0()

            If args.Length <> Me.Arguments.Count Then Throw New ArgumentException("argument count")

            Select Case Me.Operator
                Case InOperator.Nop
                    Return New InCode0() {}

                Case InOperator.GetArrayIndex
                    Dim ci = CType(args(0).Type, RkCILStruct)

                    Dim x As New InCall
                    x.Function = CType(SystemLibrary.TryLoadFunction(ci.FunctionNamespace, "GetValue", args.Map(Function(arg) arg.Type).ToArray), RkFunction)
                    x.Return = return_
                    x.Arguments.AddRange(args)
                    Return New InCode0() {x}

                Case Else
                    Dim x As New InCode With {.Operator = Me.Operator, .Left = If(args.Length > 0, args(0), Nothing), .Right = If(args.Length > 1, args(1), Nothing), .Return = return_}
                    Return New InCode0() {x}

            End Select
        End Function

        Public Overrides Function CloneGeneric() As IType

            Dim x = New RkNativeFunction With {.Name = Me.Name, .Operator = Me.Operator, .Scope = Me.Scope, .GenericBase = Me, .Parent = Me}
            x.Scope.AddFunction(x)
            Return x
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} '{Me.Operator}'"
        End Function

    End Class

End Namespace
