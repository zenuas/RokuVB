Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkNamespace
        Implements IEntry, IAddStruct, IAddFunction


        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Structs As New Dictionary(Of String, List(Of RkStruct))
        Public Overridable ReadOnly Property Functions As New Dictionary(Of String, List(Of RkFunction))
        Public Overridable ReadOnly Property LoadPaths As New List(Of IEntry)

        Public Overridable Sub AddLoadPath(path As IEntry)

            ' load path format
            ' ok "use System"
            ' ng "use System.*" -> "use System"
            ' -- "use System.Int"
            ' -- "use System.Math.max"

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

        Public Overridable Function LoadFunction(name As String, ParamArray args() As IType) As RkFunction

            Dim x = Me.TryLoadFunction(name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function TryLoadFunction(name As String, ParamArray args() As IType) As RkFunction

            If Me.Functions.ContainsKey(name) Then

                Dim x = Me.TryGetFunction(name, args)
                If x IsNot Nothing Then Return x
            End If

            For Each path In Me.LoadPaths

                If TypeOf path Is RkFunction Then

                    Dim func = CType(path, RkFunction)
                    If func.Name.Equals(name) Then Return func

                ElseIf TypeOf path Is RkNamespace Then

                    Dim x = CType(path, RkNamespace).TryLoadFunction(name, args)
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

        Public Overridable Sub AddFunction(x As RkFunction) Implements IAddFunction.AddFunction

            Dim name = x.Name
            If Not Me.Functions.ContainsKey(name) Then Me.Functions.Add(name, New List(Of RkFunction))
            Me.Functions(name).Add(x)
        End Sub

        Public Overridable Function GetStruct(name As String, ParamArray args() As IType) As RkStruct Implements IAddStruct.GetStruct

            Dim x = Me.TryGetStruct(name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function TryGetStruct(name As String, ParamArray args() As IType) As RkStruct

            For Each f In Me.Structs(name).Where(Function(x) x.Apply.Count = args.Length AndAlso Not x.HasGeneric)

                If f.Apply.And(Function(x, i) x Is args(i)) Then Return f
            Next

            For Each f In Me.Structs(name).Where(Function(x) x.Generics.Count = args.Length AndAlso x.HasGeneric)

                Return CType(f.FixedGeneric(args), RkStruct)
            Next

            Return Nothing
        End Function

        Public Overridable Function GetFunction(name As String, ParamArray args() As IType) As RkFunction Implements IAddFunction.GetFunction

            Dim x = Me.TryGetFunction(name, args)
            If x IsNot Nothing Then Return x

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function TryGetFunction(name As String, ParamArray args() As IType) As RkFunction

            For Each f In Me.Functions(name).Where(Function(x) x.Arguments.Count = args.Length AndAlso Not x.HasGeneric)

                If f.Arguments.And(Function(x, i) x.Value Is args(i)) Then Return f
            Next

            Dim generic_match As Action(Of IType, IType, Action(Of String, IType)) =
                Sub(arg, p, f)

                    If TypeOf arg Is RkGenericEntry Then

                        f(arg.Name, p)

                    ElseIf arg.HasGeneric AndAlso arg.Namespace Is p.Namespace AndAlso arg.Name.Equals(p.Name) Then

                        Dim struct = CType(arg, RkStruct)
                        struct.Generics.Do(Sub(x, i) generic_match(x, CType(CType(p, RkStruct).Apply(i), RkStruct), f))
                    End If
                End Sub

            For Each f In Me.Functions(name).Where(Function(x) x.Arguments.Count = args.Length AndAlso x.HasGeneric AndAlso x.Arguments.And(Function(arg, i) arg.Value.Is(args(i))))

                Dim xs(f.Generics.Count - 1) As IType
                For i = 0 To f.Arguments.Count - 1

                    generic_match(f.Arguments(i).Value, args(i),
                        Sub(atname, p)

                            Dim xs_i = f.Generics.IndexOf(Function(g) g.Name.Equals(atname))
                            If xs(xs_i) Is Nothing Then

                                xs(xs_i) = p
                            Else

                                Debug.Assert(xs(xs_i) Is p)
                            End If
                        End Sub)
                Next

                Return CType(f.FixedGeneric(xs), RkFunction)
            Next

            Return Nothing
        End Function

        'Public Overridable Function GetValueOf(Of T)(name As String, default_ As Action) As T

        '    Return Me.GetValueOf(Of T)(name,
        '        Function()

        '            default_()
        '        End Function)
        'End Function

        'Public Overridable Function GetValueOf(Of T)(name As String, default_ As Func(Of T)) As T

        '    Dim x As IEntry = Nothing
        '    If Not Me.Local.TryGetValue(name, x) OrElse TypeOf x IsNot T Then Return default_()
        '    'If Not Me.Local.TryGetValue(name, x) OrElse TypeOf x IsNot T Then

        '    '    ' demangling
        '    '    For Each v In Me.Local.Values

        '    '        If v IsNot Nothing AndAlso v.Name.Equals(name) Then

        '    '            x = v
        '    '            Exit For
        '    '        End If
        '    '    Next
        '    '    If x Is Nothing OrElse TypeOf x IsNot T Then Return default_()
        '    'End If
        '    Return CType(x, T)
        'End Function

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} {Me.Name}"
        End Function
    End Class

End Namespace
