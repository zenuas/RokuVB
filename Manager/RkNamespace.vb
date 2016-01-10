Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkNamespace
        Implements IEntry, IAddStruct, IAddFunction


        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IEntry)
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

            Me.Local.Add(x.Name, x)
        End Sub

        Public Overridable Function GetFunction(name As String, ParamArray args() As IType) As RkFunction Implements IAddFunction.GetFunction

            Dim f = Me.GetValueOf(Of RkFunction)(name, Sub() Throw New ArgumentException($"``{name}'' was not found"))

            If f.Arguments.Count <> args.Length Then Throw New ArgumentException("parameter miss match")
            If f.HasGeneric Then

                Dim xs = Util.Functions.List(Util.Functions.Map(f.Generics, Function(x) CType(Nothing, IType)))
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

                Return CType(f.FixedGeneric(xs.ToArray), RkFunction)
            Else

                ' check
                Return f
            End If
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
            Return CType(x, T)
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.GetType.Name} {Me.Name}"
        End Function
    End Class

End Namespace
