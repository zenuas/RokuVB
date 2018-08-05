Imports System
Imports System.Collections.Generic
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkTuple
        Inherits RkScope
        Implements IStruct, IAddLet

        Public Overridable Property Super As IType
        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overrides Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IType)
        Public Overridable ReadOnly Property Apply As New List(Of IType) Implements IApply.Apply


        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            If Me.Local.ContainsKey(name) Then Return Me.Local(name)
            If Me.Super IsNot Nothing Then Return Me.Super.GetValue(name)
            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            If t Is Nothing Then Return False
            If TypeOf t Is RkByName Then Return Me.Is(CType(t, RkByName).Type)
            If TypeOf t Is RkUnionType Then Return t.Is(Me)
            If TypeOf t Is RkClass Then Return t.Is(Me)

            If Me Is t Then Return True
            If Me.Scope Is t.Scope AndAlso Me.Name.Equals(t.Name) Then Return True

            Return False
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Throw New NotImplementedException
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException
        End Function

        Public Overridable Function TypeToApply(value As IType) As IType() Implements IType.TypeToApply

            Throw New NotImplementedException()
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return False
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException
        End Function

        Public Overridable Sub AddLet(name As String, t As IType) Implements IAddLet.AddLet

            Me.Local.Add(name, t)
        End Sub

        Public Overridable Function CreateManglingName() As String

            Dim s = Me.Name
            Dim p = Me.Parent
            Do While TypeOf p IsNot RkNamespace

                s = $"{p.Name}#{s}"
                p = p.Parent
            Loop
            Return s
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return Me.Apply.Or(Function(x) x IsNot Nothing AndAlso x.HasIndefinite)
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Name}{If(Me.Apply.IsNull, "", $"{"(" + String.Join(", ", Me.Apply.Map(Function(x) If(x Is Nothing, "_", x.ToString))) + ")"}")}"
        End Function

    End Class

End Namespace
