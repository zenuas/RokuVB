Imports System.Collections.Generic
Imports System.Reflection


Namespace Architecture

    Public Class TypeData

        Public Overridable Property Type As System.Type
        Public Overridable Property Constructor As ConstructorInfo
        Public Overridable Property Fields As New Dictionary(Of String, FieldInfo)
        Public Overridable Property Methods As New Dictionary(Of String, MethodInfo)

        Public Overridable Function GetField(name As String) As FieldInfo

            Return If(Me.Fields.ContainsKey(name), Me.Fields(name), Me.Type.GetField(name))
        End Function

        Public Overridable Function GetMethod(name As String) As MethodInfo

            Return If(Me.Methods.ContainsKey(name), Me.Methods(name), Me.Type.GetMethod(name))
        End Function
    End Class

End Namespace
