Imports System
Imports System.Collections.Generic
Imports Roku.Manager
Imports Roku.Util.Extensions
Imports Roku.Util.TypeHelper


Namespace Node

    Public Class ProgramNode
        Inherits FunctionNode

        Public Sub New()
            MyBase.New(1)

            Me.Name = ".ctor"
            Me.InnerScope = False
        End Sub

        Public Overridable Property FileName As String = ""
        Public Overridable ReadOnly Property Uses As New List(Of UseNode)
        Public Overridable ReadOnly Property FixedGenericFunction As New Dictionary(Of IFunction, FunctionNode)

        Public Overridable Sub AddFixedGenericFunction(f As IFunction)

            If Me.FixedGenericFunction.ContainsKey(f) Then

                f.FunctionNode = Me.FixedGenericFunction(f)
            Else

                Dim base = If(f.GenericBase?.FunctionNode, f.FunctionNode)
                Dim bind = base.Bind
                Dim parent = base.Parent
                base.Bind = Nothing
                base.Parent = Nothing
                Dim clone = CType(NodeDeepCopy(base), FunctionNode)
                base.Bind = bind
                base.Parent = parent
                clone.Bind = bind
                clone.Parent = parent

                For i = 0 To clone.Arguments.Count - 1

                    SetNodeType(clone.Arguments(i).Type, f.Arguments(i).Value)
                Next
                If clone.Return IsNot Nothing Then SetNodeType(clone.Return, f.Return)
                clone.Type = f

                f.FunctionNode = clone
                Me.FixedGenericFunction.Add(f, clone)
            End If
        End Sub

        Public Shared Sub SetNodeType(base As TypeBaseNode, t As IType)

            If TypeOf base Is TypeNode AndAlso TypeOf t Is IApply Then

                Dim apply = CType(t, IApply)
                CType(base, TypeNode).Arguments.Each(Sub(x) x.Type = If(apply.Apply.FindFirstOrNull(Function(a) a.Name.Equals(x.Name)), x.Type))
            End If
            base.Type = t
        End Sub

        Public Shared Function NodeDeepCopy(n As INode) As INode

            Dim cache As New Dictionary(Of INode, INode)
            Dim copy As Func(Of INode, INode) =
                Function(v)

                    If cache.ContainsKey(v) Then Return cache(v)
                    Dim clone = v.Clone
                    cache(v) = clone

                    For Each p In Util.Traverse.Fields(clone)

                        If TypeOf p.Item1 Is INode Then

                            p.Item2.SetValue(clone, copy(CType(p.Item1, INode)))

                        ElseIf p.Item1 IsNot Nothing Then

                            Dim t = p.Item2.FieldType
                            If t.IsArray AndAlso IsInterface(t.GetElementType, GetType(INode)) Then

                                Dim arr = CType(CType(p.Item1, Array).Clone, Array)
                                For i = 0 To arr.Length - 1

                                    arr.SetValue(copy(CType(arr.GetValue(i), INode)), i)
                                Next
                                p.Item2.SetValue(clone, arr)

                            ElseIf IsGeneric(t, GetType(List(Of ))) AndAlso IsInterface(t.GenericTypeArguments(0), GetType(INode)) Then

                                Dim base = CType(p.Item1, System.Collections.IList)
                                Dim arr = CType(Activator.CreateInstance(GetType(List(Of )).MakeGenericType(t.GenericTypeArguments(0))), System.Collections.IList)
                                For i = 0 To base.Count - 1

                                    arr.Add(copy(CType(base(i), INode)))
                                Next
                                p.Item2.SetValue(clone, arr)

                            ElseIf IsGeneric(t, GetType(Dictionary(Of ,))) AndAlso (IsInterface(t.GenericTypeArguments(0), GetType(INode)) OrElse IsInterface(t.GenericTypeArguments(1), GetType(INode))) Then

                                Dim base = CType(p.Item1, System.Collections.IDictionary)
                                Dim hash = CType(Activator.CreateInstance(GetType(Dictionary(Of ,)).MakeGenericType(t.GenericTypeArguments(0), t.GenericTypeArguments(1))), System.Collections.IDictionary)

                                For Each key In base.Keys

                                    Dim value = base(key)
                                    If TypeOf key Is INode Then key = copy(CType(key, INode))
                                    If TypeOf value Is INode Then value = copy(CType(value, INode))

                                    hash(key) = value
                                Next
                                p.Item2.SetValue(clone, hash)

                            ElseIf TypeOf p.Item1 Is ICloneable Then

                                p.Item2.SetValue(clone, CType(p.Item1, ICloneable).Clone(Function(x) copy(x)))
                            End If
                        End If
                    Next

                    Return clone
                End Function

            Return copy(n)
        End Function

    End Class

End Namespace
