Imports System
Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Operator
Imports Roku.IntermediateCode
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkFunction
        Implements IType, IApply

        Public Overridable Property [Namespace] As RkNamespace Implements IType.Namespace
        Public Overridable Property Name As String Implements IEntry.Name
        Public Overridable ReadOnly Property Arguments As New List(Of NamedValue)
        Public Overridable Property [Return] As IType
        Public Overridable ReadOnly Property Body As New List(Of InCode0)
        Public Overridable ReadOnly Property Generics As New List(Of RkGenericEntry)
        Public Overridable Property GenericBase As RkFunction = Nothing
        Public Overridable ReadOnly Property Apply As New List(Of IType) Implements IApply.Apply
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

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            Return Me Is t
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Dim x = Me.Generics.Find(Function(a) a.Name.Equals(name))
            If x IsNot Nothing Then Return x

            x = New RkGenericEntry With {.Name = name, .Namespace = Me.Namespace, .ApplyIndex = Me.Generics.Count}
            Me.Generics.Add(x)
            Me.Apply.Add(Nothing)
            Return x
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Throw New Exception("not generics")
            If Me.Generics.Count <> values.Length Then Throw New ArgumentException("generics count miss match")

            Return Me.FixedGeneric(values.Map(Function(v, i) New NamedValue With {.Name = Me.Generics(i).Name, .Value = v}).ToArray)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            If Not Me.HasGeneric Then Return Me

            Dim apply_map As New Dictionary(Of Integer, NamedValue)
            Me.Generics.Do(Sub(x) apply_map(x.ApplyIndex) = values.FindFirst(Function(v) v.Name.Equals(x.Name)))
            Dim apply = Me.Apply.Map(Function(x, i) If(apply_map.ContainsKey(i), apply_map(i).Value, x)).ToArray
            For Each fix In Me.GetBaseFunctions().Where(Function(g) g.Apply.Count = apply.Length)

                If fix.Apply.And(Function(g, i) apply(i) Is g) Then Return fix
            Next

            Dim apply_fix =
                Function(c As IType)

                    If Not c.HasGeneric Then

                        Return c

                    ElseIf TypeOf c Is IApply Then

                        Return c.FixedGeneric(CType(c, IApply).Apply.Map(Function(x) values.FindFirst(Function(v) v.Name.Equals(x.Name)).Value).ToArray)
                    Else
                        Return c.FixedGeneric(values)
                    End If
                End Function

            Dim clone = CType(Me.CloneGeneric, RkFunction)
            values = values.Map(Function(v) New NamedValue With {.Name = v.Name, .Value = If(TypeOf v.Value Is RkGenericEntry, clone.DefineGeneric(v.Name), v.Value)}).ToArray
            If Me.Return IsNot Nothing Then clone.Return = apply_fix(Me.Return)
            Me.Arguments.Do(Sub(v, i) clone.Arguments.Add(New NamedValue With {.Name = v.Name, .Value = apply_fix(v.Value)}))
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

            Dim x = New RkFunction With {.Name = Me.Name, .Namespace = Me.Namespace, .GenericBase = Me}
            x.Namespace.AddFunction(x)
            Return x
        End Function

        Public Overridable Function GetBaseFunctions() As List(Of RkFunction)

            If Me.Namespace.Functions.ContainsKey(Me.Name) AndAlso Me.Namespace.Functions(Me.Name).Exists(Function(s) s Is Me) Then Return Me.Namespace.Functions(Me.Name)
            Return Me.Namespace.Functions.FindFirst(Function(x) x.Value.Exists(Function(s) s Is Me)).Value
        End Function

        Public Overridable Function CreateCall(self As OpValue, ParamArray args() As OpValue) As InCode0()

            'Debug.Assert(Me.Closure IsNot Nothing OrElse Me.Arguments.Count = args.Length, "unmatch arguments count")
            Dim x As InCall = If(Me.IsAnonymous, New InLambdaCall With {.Value = self}, New InCall)
            x.Function = Me
            x.Arguments.AddRange(args)
            Return New InCode0() {x}
        End Function

        Public Overridable Function CreateCallReturn(self As OpValue, return_ As OpValue, ParamArray args() As OpValue) As InCode0()

            'Debug.Assert(Me.Closure IsNot Nothing OrElse Me.Arguments.Count = args.Length, "unmatch arguments count")
            Dim x As InCall = If(Me.IsAnonymous, New InLambdaCall With {.Value = self}, New InCall)
            x.Function = Me
            x.Return = return_
            x.Arguments.AddRange(args)
            Return New InCode0() {x}
        End Function

        Public Overridable Function CreateManglingName() As String

            Return Me.ToString
        End Function

        Public Overrides Function ToString() As String

            Return $"{Me.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.Value.ToString))})" + If(Me.Return IsNot Nothing, $" {Me.Return.ToString}", "")
        End Function
    End Class

End Namespace
