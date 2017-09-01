Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkNamespace
        Implements IType, IAddStruct, IAddFunction, IAddNamespace


        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable Property Parent As RkNamespace
        Public Overridable ReadOnly Property Structs As New Dictionary(Of String, List(Of RkStruct))
        Public Overridable ReadOnly Property Functions As New Dictionary(Of String, List(Of IFunction))
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

        Public Overridable Function LoadStruct(name As String, ParamArray args() As IType) As RkStruct

            Dim x = Me.TryLoadStruct(name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function TryLoadStruct(name As String, ParamArray args() As IType) As RkStruct

            If Me.Structs.ContainsKey(name) Then

                Dim x = Me.TryGetStruct(name, args)
                If x IsNot Nothing Then Return x
            End If

            For Each path In Me.LoadPaths

                If TypeOf path Is RkStruct Then

                    Dim struct = CType(path, RkStruct)
                    If struct.Name.Equals(name) Then Return struct

                ElseIf TypeOf path Is RkNamespace Then

                    Dim x = CType(path, RkNamespace).TryLoadStruct(name, args)
                    If x IsNot Nothing Then Return x
                End If
            Next

            Return Nothing
        End Function

        Public Overridable Iterator Function FindLoadFunction(name As String, match As Func(Of IFunction, Boolean)) As IEnumerable(Of IFunction)

            For Each f In Me.FindCurrentFunction(name)

                If match(f) Then Yield f
            Next

            For Each path In Me.LoadPaths

                If TypeOf path Is RkFunction Then

                    Dim func = CType(path, RkFunction)
                    If func.Name.Equals(name) AndAlso match(func) Then Yield func

                ElseIf TypeOf path Is RkNamespace Then

                    For Each f In CType(path, RkNamespace).FindLoadFunction(name, match)

                        Yield f
                    Next
                End If
            Next

        End Function

        Public Overridable Iterator Function FindLoadFunction(name As String, ParamArray args() As IType) As IEnumerable(Of IFunction)

            For Each f In Me.FindLoadFunction(name, Function(x) Not x.HasIndefinite AndAlso x.Arguments.Count = args.Length AndAlso x.Arguments.And(Function(arg, i) TypeOf args(i) Is RkGenericEntry OrElse arg.Value.Is(args(i))))

                Yield f
            Next

        End Function

        Public Overridable Iterator Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction)

            If Me.Functions.ContainsKey(name) Then

                For Each f In Me.Functions(name)

                    Yield f
                Next
            End If

        End Function

        Public Overridable Function LoadFunction(name As String, ParamArray args() As IType) As IFunction

            Dim x = Me.TryLoadFunction(name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function TryLoadFunction(name As String, ParamArray args() As IType) As IFunction

            Return Me.MergeLoadFunctions(Me.FindLoadFunction(name, args).ToList, args)
        End Function

        Public Overridable Function MergeLoadFunctions(fs As IList(Of IFunction), ParamArray args() As IType) As IFunction

            If fs.Count = 0 Then

                Return Nothing

            ElseIf fs.Count = 1 Then

                Return fs(0).ApplyFunction(args)
            Else

                'Dim f = fs.FindFirstOrNull(Function(x) Not x.HasGeneric)
                'If f IsNot Nothing Then Return f

                fs.Do(Function(x) x.ApplyFunction(args))
                fs = fs.ToHash_ValueDerivation(Function(x) True).Keys.ToList
                If fs.Count <= 1 Then Return Me.MergeLoadFunctions(fs, args)

                Return New RkSomeType(fs)
            End If
        End Function

        Public Overridable Function LoadNamespace(name As String) As RkNamespace

            Dim x = Me.TryLoadNamespace(name)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function TryLoadNamespace(name As String) As RkNamespace

            If Me.Namespaces.ContainsKey(name) Then

                Dim x = Me.TryGetNamespace(name)
                If x IsNot Nothing Then Return x
            End If

            For Each path In Me.LoadPaths

                If TypeOf path Is RkNamespace Then

                    Dim ns = CType(path, RkNamespace)
                    If ns.Name.Equals(name) Then Return ns
                    Dim x = ns.TryLoadNamespace(name)
                    If x IsNot Nothing Then Return x
                End If
            Next

            Return Nothing
        End Function

        Public Overridable Sub AddStruct(x As RkStruct) Implements IAddStruct.AddStruct

            Me.AddStruct(x, x.Name)
        End Sub

        Public Overridable Sub AddStruct(x As RkStruct, name As String) Implements IAddStruct.AddStruct

            If Not Me.Structs.ContainsKey(name) Then Me.Structs.Add(name, New List(Of RkStruct))
            Me.Structs(name).Add(x)
        End Sub

        Public Overridable Sub AddFunction(x As IFunction) Implements IAddFunction.AddFunction

            Me.AddFunction(x, x.Name)
        End Sub

        Public Overridable Sub AddFunction(x As IFunction, name As String) Implements IAddFunction.AddFunction

            If Not Me.Functions.ContainsKey(name) Then Me.Functions.Add(name, New List(Of IFunction))
            Me.Functions(name).Add(x)
        End Sub

        Public Overridable Function TryGetStruct(name As String, ParamArray args() As IType) As RkStruct

            If Not Me.Structs.ContainsKey(name) Then Return Nothing

            For Each f In Me.Structs(name).Where(Function(x) x.Apply.Count = args.Length AndAlso Not x.HasGeneric)

                If f.Apply.And(Function(x, i) x Is args(i)) Then Return f
            Next

            For Each f In Me.Structs(name).Where(Function(x) x.Generics.Count = args.Length AndAlso x.HasGeneric)

                Return CType(f.FixedGeneric(args), RkStruct)
            Next

            Return Nothing
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

            If Me.Parent Is Nothing Then Return $"{Me.Name}"
            Return $"{Me.Parent.FullName}.{Me.Name}"
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.FullName}"
        End Function

        Public Overridable Property [Namespace] As RkNamespace Implements IType.Namespace
            Get
                Return Me
            End Get
            Set(value As RkNamespace)

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
