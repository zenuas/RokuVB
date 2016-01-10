Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkNamespace
        Implements IEntry, IAddStruct, IAddFunction


        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IEntry)
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

        Public Overridable Function LoadLibrary(name As String) As IType

            If Me.Local.ContainsKey(name) Then

                Dim x = Me.Local(name)
                If TypeOf x Is IType Then Return CType(x, IType)
            End If

            ' name format
            ' ok "Int"
            ' -- "System.Int"
            ' -- "System.Math.max"

            For Each path In Me.LoadPaths

                If TypeOf path Is RkStruct Then

                    Dim struct = CType(path, RkStruct)
                    If struct.Name.Equals(name) Then Return struct
                    If struct.Local.ContainsKey(name) Then Return struct.Local(name)
                End If
            Next

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Sub AddStruct(x As RkStruct) Implements IAddStruct.AddStruct

            Me.Local.Add(x.Name, x)
        End Sub

        Public Overridable Sub AddFunction(x As RkFunction) Implements IAddFunction.AddFunction

            Dim name = x.Name
            If Not Me.Functions.ContainsKey(name) Then Me.Functions.Add(name, New List(Of RkFunction))
            Me.Functions(name).Add(x)

            'Dim name = x.CreateManglingName
            'If Me.Local.ContainsKey(name) Then

            '    If Me.Local(name) IsNot x Then

            '        ' check
            '    End If
            'Else

            '    Me.Local.Add(name, x)
            'End If
        End Sub

        Public Overridable Function GetFunction(name As String, ParamArray args() As IType) As RkFunction Implements IAddFunction.GetFunction

            For Each f In Util.Functions.Where(Me.Functions(name), Function(x) x.Arguments.Count = args.Length AndAlso Not x.HasGeneric)

                If Util.Functions.And(f.Arguments, Function(x, i) x.Value Is args(i)) Then Return f
            Next

            For Each f In Util.Functions.Where(Me.Functions(name), Function(x) x.Arguments.Count = args.Length AndAlso x.HasGeneric)

                Dim xs(f.Generics.Count - 1) As IType
                For i = 0 To f.Arguments.Count - 1

                    Dim arg = f.Arguments(i)
                    If TypeOf arg.Value IsNot RkGenericEntry Then Continue For

                    Dim xs_i = Util.Functions.IndexOf(f.Generics, Function(g) g.Name.Equals(arg.Value.Name))
                    If xs(xs_i) Is Nothing Then

                        xs(xs_i) = args(i)
                    Else

                        If xs(xs_i) IsNot args(i) Then

                        End If
                    End If
                Next

                Dim x = CType(f.FixedGeneric(xs), RkFunction)
                Me.AddFunction(x)
                Return x
            Next

            Throw New ArgumentException($"``{name}'' was not found")
        End Function

        Public Overridable Function GetValueOf(Of T)(name As String, default_ As Action) As T

            Return Me.GetValueOf(Of T)(name,
                Function()

                    default_()
                End Function)
        End Function

        Public Overridable Function GetValueOf(Of T)(name As String, default_ As Func(Of T)) As T

            Dim x As IEntry = Nothing
            If Not Me.Local.TryGetValue(name, x) OrElse TypeOf x IsNot T Then Return default_()
            'If Not Me.Local.TryGetValue(name, x) OrElse TypeOf x IsNot T Then

            '    ' demangling
            '    For Each v In Me.Local.Values

            '        If v IsNot Nothing AndAlso v.Name.Equals(name) Then

            '            x = v
            '            Exit For
            '        End If
            '    Next
            '    If x Is Nothing OrElse TypeOf x IsNot T Then Return default_()
            'End If
            Return CType(x, T)
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} {Me.Name}"
        End Function
    End Class

End Namespace
