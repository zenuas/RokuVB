Imports System
Imports System.Collections.Generic
Imports Roku.Node


Namespace Manager

    Public Class RkFunction
        Implements IType

        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Arguments As New List(Of NamedValue)
        Public Overridable Property [Return] As IType
        Public Overridable ReadOnly Property Body As New List(Of RkCode0)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable Property Apply As IEnumerable(Of IType) = Nothing
        Public Overridable ReadOnly Property Fixed As New List(Of RkFunction)


        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.Find(Function(a) a.Name.Equals(name))
            If x IsNot Nothing Then Return x

            x = New RkGenericEntry With {.Name = name}
            Me.Generics.Add(x)
            Return x
        End Function

        Public Function FixedGenericCall(fcall As FunctionCallNode) As IType

            Dim xs As New Dictionary(Of String, IType)
            For i = 0 To Me.Arguments.Count - 1

                Dim arg = Me.Arguments(i)
                If TypeOf arg.Value IsNot RkGenericEntry Then Continue For

                If xs.ContainsKey(arg.Value.Name) Then

                    ' type check
                Else
                    xs.Add(arg.Value.Name, fcall.Arguments(i).Type)
                End If
            Next

            Return Me.FixedGeneric(Util.Functions.List(Util.Functions.Map(xs.Keys, Function(x) New NamedValue With {.Name = x, .Value = xs(x)})).ToArray)

        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(Util.Functions.List(Util.Functions.Map(values, Function(v, i) New NamedValue With {.Name = Me.Generics(i).Name, .Value = v})).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Return Me

            For Each fix In Me.Fixed

                If Util.Functions.And(fix.Apply, Function(g, i) Util.Functions.Find(values, Function(v) v.Name.Equals(Me.Generics(i).Name)).Value Is g) Then Return fix
            Next

            Dim x = CType(Me.CloneGeneric, RkFunction)
            If Me.Return IsNot Nothing Then x.Return = Me.Return.FixedGeneric(values)
            For Each v In Me.Arguments

                x.Arguments.Add(New NamedValue With {.Name = v.Name, .Value = v.Value.FixedGeneric(values)})
            Next
            x.Body.AddRange(Me.Body)
            x.Apply = Util.Functions.Map(Me.Generics, Function(g) Util.Functions.Find(values, Function(v) v.Name.Equals(g.Name)).Value)
            Me.Fixed.Add(x)
            Return x
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Return New RkFunction With {.Name = Me.Name}
        End Function

        Public Overridable Function CreateCall(ParamArray args() As RkValue) As RkCode0()

            Dim x As New RkCall With {.Function = Me}
            x.Arguments.AddRange(args)
            Return New RkCode0() {x}
        End Function

        Public Overridable Function CreateCallReturn(return_ As RkValue, ParamArray args() As RkValue) As RkCode0()

            Dim x As New RkCall With {.Function = Me, .Return = return_}
            x.Arguments.AddRange(args)
            Return New RkCode0() {x}
        End Function

        Public Overridable Function CreateManglingName() As String

            Return Me.ToString
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Name}({String.Join(", ", Util.Functions.Map(Me.Arguments, Function(x) x.Value.Name))})" + If(Me.Return IsNot Nothing, $" {Me.Return.Name}", "")
        End Function
    End Class

End Namespace
