Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkNamespace
        Implements IEntry


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

        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IEntry)
        Public Overridable ReadOnly Property LoadPaths As New List(Of IEntry)

    End Class

End Namespace
