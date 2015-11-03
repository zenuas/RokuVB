Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkGeneric
        Implements IType

        Public Overridable Property Name As String Implements IType.Name
        Public Overridable ReadOnly Property Local As New Dictionary(Of String, IType) Implements IType.Local
        Public Overridable ReadOnly Property Generics As List(Of RkGenericEntry)

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry

            Dim x As New RkGenericEntry With {.Name = name}
            Me.Generics.Add(x)
            Return x
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As RkStruct

            Return Nothing
        End Function

    End Class

End Namespace
