Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Manager

    Public Class RkFunction
        Implements IType

        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Arguments As New List(Of NamedValue)
        Public Overridable Property [Return] As IType
        Public Overridable ReadOnly Property Body As New List(Of RkCode0)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)


        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x As New RkGenericEntry With {.Name = name}
            Me.Generics.Add(x)
            Return x
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(Util.Functions.List(Util.Functions.Map(values, Function(v, i) New NamedValue With {.Name = Me.Generics(i).Name, .Value = v})).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Return Me

            Dim x As New RkFunction With {.Name = Me.Name}
            If Me.Return IsNot Nothing Then x.Return = Me.Return.FixedGeneric(values)
            For Each v In Me.Arguments

                x.Arguments.Add(New NamedValue With {.Name = v.Name, .Value = v.Value.FixedGeneric(values)})
            Next
            x.Body.AddRange(Me.Body)
            Return x
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0
        End Function

        Public Overridable Function CreateCall(ParamArray args() As RkValue) As RkCode0()

            Dim x As New RkCall With {.Function = New RkValue With {.Type = Me}}
            x.Arguments.AddRange(args)
            Return New RkCode0() {x}
        End Function

        Public Overridable Function CreateCallReturn(return_ As RkValue, ParamArray args() As RkValue) As RkCode0()

            Dim x As New RkCall With {.Function = New RkValue With {.Type = Me}, .Return = return_}
            x.Arguments.AddRange(args)
            Return New RkCode0() {x}
        End Function

    End Class

End Namespace
