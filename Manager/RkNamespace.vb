Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkNamespace
        Inherits RkScope
        Implements IType, IAddNamespace


        Public Overrides Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Namespaces As New Dictionary(Of String, RkNamespace)
        Public Overridable ReadOnly Property LoadPaths As New List(Of IEntry)

        Public Overridable Sub AddLoadPath(path As IEntry)

            ' load path format
            ' ok "use System"
            ' ng "use System.*" -> "use System"
            ' -- "use System.Int"
            ' -- "use System.Math.max"

            Debug.Assert(path IsNot Nothing, "loadpath is null")
            If Not Me.LoadPaths.Contains(path) AndAlso Me IsNot path Then Me.LoadPaths.Add(path)
        End Sub

        Public Overrides Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction)

            Return MyBase.FindCurrentFunction(name).Where(Function(x) Not x.HasIndefinite)
        End Function

        Public Overridable Sub AddNamespace(x As RkNamespace) Implements IAddNamespace.AddNamespace

            Me.Namespaces.Add(x.Name, x)
        End Sub

        Public Overridable Function TryGetNamespace(name As String) As RkNamespace

            Return Me.Namespaces.FindFirstOrNull(Function(x) x.Key.Equals(name)).Value
        End Function

        Public Overridable Function TryGetNamespace(names As IEnumerable(Of String)) As RkNamespace

            If names.IsNull Then Return Me
            Dim first = names.First
            Return Me.TryGetNamespace(first)?.TryGetNamespace(names.Cdr)
        End Function

        Public Overridable Function FullName() As String

            If Me.Parent Is Nothing Then Return Me.Name
            Return $"{CType(Me.Parent, RkNamespace).FullName}.{Me.Name}"
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.FullName}"
        End Function

        Public Overridable Property Scope As IScope Implements IType.Scope
            Get
                Return Me
            End Get
            Set(value As IScope)

                Throw New NotSupportedException()
            End Set
        End Property

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return False
        End Function
    End Class

End Namespace
