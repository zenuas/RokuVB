Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkFunction
        Implements IType

        Public Overridable Property [Namespace] As RkNamespace Implements IType.Namespace
        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Arguments As New List(Of NamedValue)
        Public Overridable Property [Return] As IType
        Public Overridable ReadOnly Property Body As New List(Of RkCode0)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable ReadOnly Property Apply As New List(Of IType)
        Public Overridable Property FunctionNode As FunctionNode = Nothing
        Public Overridable Property Closure As RkStruct = Nothing


        Public Overridable ReadOnly Property IsAnonymous As Boolean
            Get
                Return String.IsNullOrEmpty(Me.Name)
            End Get
        End Property

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.Find(Function(a) a.Name.Equals(name))
            If x IsNot Nothing Then Return x

            x = New RkGenericEntry With {.Name = name, .Namespace = Me.Namespace, .ApplyIndex = Me.Generics.Count}
            Me.Generics.Add(x)
            Me.Apply.Add(Nothing)
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

            Return Me.FixedGeneric(xs.Keys.Map(Function(x) New NamedValue With {.Name = x, .Value = xs(x)}).ToArray)

        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(values.Map(Function(v, i) New NamedValue With {.Name = Me.Generics(i).Name, .Value = v}).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Return Me

            Dim apply_map = values.ToHash_KeyDerivation(Function(x) Me.Generics.FindFirst(Function(g) g.Name.Equals(x.Name)).ApplyIndex)
            Dim apply = Me.Apply.Map(Function(x, i) If(apply_map.ContainsKey(i), apply_map(i).Value, x)).ToArray
            For Each fix In Me.Namespace.Functions(Me.Name).Where(Function(g) g.Apply.Count = apply.Length)

                If fix.Apply.And(Function(g, i) apply(i) Is g) Then Return fix
            Next

            Dim clone = CType(Me.CloneGeneric, RkFunction)
            values = values.Map(Function(v) New NamedValue With {.Name = v.Name, .Value = If(TypeOf v.Value Is RkGenericEntry, clone.DefineGeneric(v.Name), v.Value)}).ToArray
            If Me.Return IsNot Nothing Then clone.Return = Me.Return.FixedGeneric(values)
            For Each v In Me.Arguments

                clone.Arguments.Add(New NamedValue With {.Name = v.Name, .Value = v.Value.FixedGeneric(values)})
            Next
            clone.Body.AddRange(Me.Body)
            clone.Apply.Clear()
            clone.Apply.AddRange(apply)
            clone.FunctionNode = Me.FunctionNode
            Return clone
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Generics.Count > 0 OrElse Me.Apply.Or(Function(x) x Is Nothing OrElse TypeOf x Is RkGenericEntry OrElse x.HasGeneric)
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Dim x = New RkFunction With {.Name = Me.Name, .Namespace = Me.Namespace}
            x.Namespace.AddFunction(x)
            Return x
        End Function

        Public Overridable Function CreateCall(self As RkValue, ParamArray args() As RkValue) As RkCode0()

            Dim x As RkCall = If(Me.IsAnonymous, New RkLambdaCall With {.Value = self}, New RkCall)
            x.Function = Me
            x.Arguments.AddRange(args)
            Return New RkCode0() {x}
        End Function

        Public Overridable Function CreateCallReturn(self As RkValue, return_ As RkValue, ParamArray args() As RkValue) As RkCode0()

            Dim x As RkCall = If(self.Name IsNot Nothing OrElse Me.IsAnonymous, New RkLambdaCall With {.Value = self}, New RkCall)
            x.Function = Me
            x.Return = return_
            x.Arguments.AddRange(args)
            Return New RkCode0() {x}
        End Function

        Public Overridable Function CreateManglingName() As String

            Return Me.ToString
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.Value.Name))})" + If(Me.Return IsNot Nothing, $" {Me.Return.Name}", "")
        End Function
    End Class

End Namespace
