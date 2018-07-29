Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkClass
        Inherits RkScope
        Implements IType

        Public Overridable Property Scope As IScope Implements IType.Scope
        Public Overrides Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable Property ClassNode As ClassNode = Nothing


        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            If t Is Nothing Then Return False
            If TypeOf t Is RkByName Then Return Me.Is(CType(t, RkByName).Type)
            If TypeOf t Is RkUnionType Then Return t.Is(Me)
            If TypeOf t Is RkGenericEntry Then Return t.Is(Me)

            If Me Is t Then Return True
            If Me.Scope Is t.Scope AndAlso Me.Name.Equals(t.Name) Then Return True

            Return False
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.FindFirstOrNull(Function(a) a.Name.Equals(name))
            If x IsNot Nothing Then Return x

            x = New RkGenericEntry With {.Name = name, .Scope = Me.Scope, .ApplyIndex = Me.Generics.Count}
            Me.Generics.Add(x)
            Return x
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException
        End Function

        Public Overridable Function TypeToApply(value As IType) As IType() Implements IType.TypeToApply

            Throw New NotImplementedException
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return True
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return False
        End Function

        Public Overrides Function ToString() As String

            Return Me.Name
        End Function

    End Class

End Namespace
