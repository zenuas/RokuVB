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

            For Each f In Me.FindLoadFunction(name, Function(x) x.Arguments.Count = args.Length AndAlso x.Arguments.And(Function(arg, i) arg.Value.Is(args(i))))

                Yield f
            Next

        End Function

        Public Overridable Iterator Function FindLoadFunction_SkipClosureArgument(name As String, ParamArray args() As IType) As IEnumerable(Of IFunction)

            For Each f In Me.FindLoadFunction(name,
                    Function(x)

                        If x.Arguments.Count = args.Length Then

                            Return x.Arguments.And(Function(arg, i) arg.Value.Is(args(i)))
                        Else

                            Dim args_without_closure = x.Arguments.Where(Function(arg) TypeOf arg.Value IsNot RkStruct OrElse Not CType(arg.Value, RkStruct).ClosureEnvironment).ToList
                            Return args_without_closure.Count = args.Length AndAlso args_without_closure.And(Function(arg, i) arg.Value.Is(args(i)))
                        End If
                    End Function)

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

        Public Overridable Function ApplyFunction(f As IFunction, ParamArray args() As IType) As IFunction

            If Not f.HasGeneric Then Return f

            Dim generic_match As Action(Of IType, IType, Action(Of RkGenericEntry, IType)) =
                Sub(arg, p, gen_to_type)

                    If TypeOf arg Is RkGenericEntry Then

                        gen_to_type(CType(arg, RkGenericEntry), p)

                    ElseIf arg.HasGeneric AndAlso arg.Namespace Is p.Namespace AndAlso arg.Name.Equals(p.Name) Then

                        Dim struct = CType(arg, RkStruct)
                        struct.Generics.Do(
                            Sub(x, i)

                                Dim apply = CType(p, RkStruct).Apply(i)
                                Dim v As RkStruct
                                If apply Is Nothing Then

                                    v = Nothing

                                ElseIf TypeOf apply Is RkStruct Then

                                    v = CType(apply, RkStruct)

                                ElseIf TypeOf apply Is RkLateBind Then

                                    v = CType(CType(apply, RkLateBind).Value, RkStruct)
                                Else

                                    Throw New Exception("unknown apply")
                                End If
                                generic_match(x, v, gen_to_type)
                            End Sub)
                    End If
                End Sub

            Dim xs(f.Generics.Count - 1) As IType
            For i = 0 To f.Arguments.Count - 1

                generic_match(f.Arguments(i).Value, args(i),
                    Sub(atname, p)

                        Dim x = xs(atname.ApplyIndex)
                        If x Is Nothing Then

                            xs(atname.ApplyIndex) = p

                        ElseIf x.Indefinite Then

                            CType(x, RkSomeType).Merge(p)

                        ElseIf p.Indefinite Then

                            CType(p, RkSomeType).Merge(x)
                            xs(atname.ApplyIndex) = p
                        Else

                            Debug.Assert(x.Is(p))
                        End If
                    End Sub)
            Next

            Return CType(f.FixedGeneric(xs), RkFunction)

        End Function

        Public Overridable Function LoadFunction(name As String, ParamArray args() As IType) As IFunction

            Dim x = Me.TryLoadFunction(name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function TryLoadFunction(name As String, ParamArray args() As IType) As IFunction

            Return Me.MergeLoadFunctions(Me.FindLoadFunction(name, args).ToList, args)
        End Function

        Public Overridable Function TryLoadFunction_SkipClosureArgument(name As String, ParamArray args() As IType) As IFunction

            Return Me.MergeLoadFunctions(Me.FindLoadFunction_SkipClosureArgument(name, args).ToList, args)
        End Function

        Public Overridable Function MergeLoadFunctions(fs As IList(Of IFunction), ParamArray args() As IType) As IFunction

            If fs.Count = 0 Then Return Nothing
            fs.Do(Function(x) Me.ApplyFunction(x, args))
            If fs.Count = 1 Then Return fs(0)

            fs = fs.Unique.ToList
            If fs.Count = 1 Then Return fs(0)

            Return New RkSomeType(fs)
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

        Public Overridable Function Indefinite() As Boolean Implements IType.Indefinite

            Return False
        End Function
    End Class

End Namespace
