Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkNamespace
        Implements IType, IAddNamespace, IScope


        Public Overridable Property Name As String Implements IEntry.Name, IScope.Name
        Public Overridable Property Parent As IScope Implements IScope.Parent
        Public Overridable ReadOnly Property Structs As New Dictionary(Of String, List(Of RkStruct)) Implements IScope.Structs
        Public Overridable ReadOnly Property Functions As New Dictionary(Of String, List(Of IFunction)) Implements IScope.Functions
        Public Overridable ReadOnly Property Namespaces As New Dictionary(Of String, RkNamespace)
        Public Overridable ReadOnly Property LoadPaths As New List(Of IEntry)

        Public Overridable Sub AddLoadPath(path As IEntry)

            ' load path format
            ' ok "use System"
            ' ng "use System.*" -> "use System"
            ' -- "use System.Int"
            ' -- "use System.Math.max"

            Debug.Assert(path IsNot Nothing, "loadpath is null")
            If Not Me.LoadPaths.Contains(path) Then Me.LoadPaths.Add(path)
        End Sub

        Public Overridable Sub AddStruct(x As RkStruct) Implements IScope.AddStruct

            Me.AddStruct(x, x.Name)
        End Sub

        Public Overridable Sub AddStruct(x As RkStruct, name As String) Implements IScope.AddStruct

            If Not Me.Structs.ContainsKey(name) Then Me.Structs.Add(name, New List(Of RkStruct))
            Me.Structs(name).Add(x)
        End Sub

        Public Overridable Iterator Function FindCurrentStruct(name As String) As IEnumerable(Of RkStruct) Implements IScope.FindCurrentStruct

            If Me.Structs.ContainsKey(name) Then

                For Each f In Me.Structs(name)

                    Yield f
                Next
            End If
        End Function

        Public Overridable Sub AddFunction(x As IFunction) Implements IScope.AddFunction

            Me.AddFunction(x, x.Name)
        End Sub

        Public Overridable Sub AddFunction(x As IFunction, name As String) Implements IScope.AddFunction

            If Not Me.Functions.ContainsKey(name) Then Me.Functions.Add(name, New List(Of IFunction))
            Me.Functions(name).Add(x)
        End Sub

        Public Overridable Iterator Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction) Implements IScope.FindCurrentFunction

            If Me.Functions.ContainsKey(name) Then

                For Each f In Me.Functions(name)

                    Yield f
                Next
            End If
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
