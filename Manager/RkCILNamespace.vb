Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Reflection
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkCILNamespace
        Inherits RkNamespace

        Public Overridable Property Root As SystemLibrary
        Public Overridable Property BaseType As RkCILStruct
        Public Overridable Property FunctionCached As Boolean = False

        Public Shared ReadOnly SpecialNames As String() = {"get_", "set_", "add_", "remove_"}
        Public Shared ReadOnly SpecialOpNames As Tuple(Of String, String)() = {
                Tuple.Create("op_Addition", "+"),
                Tuple.Create("op_Subtraction", "-"),
                Tuple.Create("op_Equality", "=="),
                Tuple.Create("op_Inequality", "!="),
                Tuple.Create("op_LessThan", "<"),
                Tuple.Create("op_LessThanOrEqual", "<="),
                Tuple.Create("op_GreaterThan", ">"),
                Tuple.Create("op_GreaterThanOrEqual", ">=")
            }

        Public Overrides Iterator Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction)

            If Not Me.FunctionCached Then

                Me.CreateFunctionCache()
                Me.FunctionCached = True
            End If

            For Each f In MyBase.FindCurrentFunction(name)

                Yield f
            Next
        End Function

        Public Overridable Sub CreateFunctionCache()

            For Each method In Me.BaseType.TypeInfo.GetMethods

                Dim method_name = method.Name
                If method.IsSpecialName Then

                    Dim prefix = SpecialNames.FindFirstOrNull(Function(x) method_name.StartsWith(x))
                    If prefix IsNot Nothing Then

                        method_name = method_name.Substring(prefix.Length)
                    Else

                        Dim op = SpecialOpNames.FindFirstOrNull(Function(x) method_name.Equals(x.Item1))
                        If op IsNot Nothing Then

                            method_name = op.Item2

                        ElseIf method_name.Equals("op_Implicit") OrElse method_name.Equals("op_Explicit") Then

                            ' not yet
                            Continue For
                        Else

                            Debug.Fail("special-name unknown case")
                        End If
                    End If
                End If

                Dim f As New RkCILFunction With {.Scope = Me, .Name = method.Name, .MethodInfo = method}
                If Not method.IsStatic Then

                    If Me.BaseType.HasGeneric Then

                        f.Arguments.Add(New NamedValue With {.Name = "self", .Value = Me.BaseType.FixedGeneric(Me.BaseType.Generics.Map(Function(x) f.DefineGeneric(x.Name)).ToArray)})
                    Else
                        f.Arguments.Add(New NamedValue With {.Name = "self", .Value = Me.BaseType})
                    End If
                End If

                Dim get_type =
                    Function(t As Type) As IType

                        Dim ti = CType(t, TypeInfo)
                        If ti.IsGenericParameter Then

                            Return f.DefineGeneric(ti.Name)
                        Else
                            Return Me.Root.LoadType(ti)
                        End If
                    End Function

                For Each arg In method.GetParameters

                    f.Arguments.Add(New NamedValue With {.Name = arg.Name, .Value = get_type(arg.ParameterType)})
                Next
                If method.ReturnType IsNot Nothing AndAlso Not method.ReturnType.Equals(GetType(System.Void)) Then f.Return = get_type(method.ReturnType)

                Me.AddFunction(f, method_name)
            Next

        End Sub

    End Class

End Namespace
