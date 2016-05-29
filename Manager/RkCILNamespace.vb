Imports System
Imports System.Collections.Generic
Imports System.Reflection


Namespace Manager

    Public Class RkCILNamespace
        Inherits RkNamespace

        Public Overridable Property Root As SystemLirary
        Public Overridable Property BaseType As RkCILStruct
        Public Overridable Property FunctionCached As Boolean = False

        Public Overrides Function LoadFunction(name As String, ParamArray args() As IType) As RkFunction

            If Not Me.FunctionCached Then

                For Each method In Me.BaseType.TypeInfo.GetMethods

                    Dim f As New RkCILFunction With {.Namespace = Me, .Name = method.Name, .MethodInfo = method}
                    If Not method.IsStatic Then f.Arguments.Add(New NamedValue With {.Name = "self", .Value = Me.BaseType})

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

                    Me.AddFunction(f)
                Next

                Me.FunctionCached = True
            End If

            Return MyBase.LoadFunction(name, args)
        End Function

    End Class

End Namespace
