Imports System
Imports System.Collections.Generic
Imports Roku.IntermediateCode
Imports Roku.Node
Imports Roku.Operator
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkUnionType
        Implements IFunction, IStruct

        Public Overridable Property UnionName As String = Nothing
        Public Overridable Property Types As List(Of IType)
        Public Overridable Property ReturnCache As RkUnionType
        Public Overridable Property UnionType As UnionTypes = UnionTypes.Subtraction

        Public Sub New()

        End Sub

        Public Sub New(xs As IEnumerable(Of IType))

            Me.Merge(xs)
        End Sub

        Public Overridable Property Name As String Implements IEntry.Name
            Get
                If Not String.IsNullOrEmpty(Me.UnionName) Then Return Me.UnionName
                Return Me.GetDecideType.Name
            End Get
            Set(value As String)

                Me.GetDecideType.Name = value
            End Set
        End Property

        Public Overridable Property Scope As IScope Implements IType.Scope
            Get
                Return Me.GetDecideType.Scope
            End Get
            Set(value As IScope)

                Me.GetDecideType.Scope = value
            End Set
        End Property

        Public Overridable ReadOnly Property Arguments As List(Of NamedValue) Implements IFunction.Arguments
            Get
                Return CType(Me.GetDecideType, IFunction).Arguments
            End Get
        End Property

        Public Overridable Property [Return] As IType Implements IFunction.Return
            Get
                If Me.Types Is Nothing OrElse Me.Types.Count = 0 Then Return Nothing
                If Me.Types.Count = 1 Then Return CType(Me.Types(0), RkFunction).Return

                If Me.ReturnCache Is Nothing Then

                    Dim xs = Me.Types.By(Of RkFunction).Where(Function(x) x.Return IsNot Nothing).ToList
                    If xs.Count = 0 Then Return Nothing
                    Me.ReturnCache = New RkUnionType(xs.Map(Function(x) x.Return)) With {.UnionType = Me.UnionType}
                End If
                Return Me.ReturnCache
            End Get
            Set(value As IType)

                Throw New NotImplementedException()
            End Set
        End Property

        Public Overridable ReadOnly Property Body As List(Of InCode0) Implements IFunction.Body
            Get
                Return CType(Me.GetDecideType, IFunction).Body
            End Get
        End Property

        Public Overridable ReadOnly Property Generics As List(Of RkGenericEntry) Implements IFunction.Generics
            Get
                Return CType(Me.GetDecideType, IFunction).Generics
            End Get
        End Property

        Public Overridable Property GenericBase As RkFunction Implements IFunction.GenericBase
            Get
                Return CType(Me.GetDecideType, IFunction).GenericBase
            End Get
            Set(value As RkFunction)

                CType(Me.GetDecideType, IFunction).GenericBase = value
            End Set
        End Property

        Public Overridable Property FunctionNode As FunctionNode Implements IFunction.FunctionNode
            Get
                Return CType(Me.GetDecideType, IFunction).FunctionNode
            End Get
            Set(value As FunctionNode)

                CType(Me.GetDecideType, IFunction).FunctionNode = value
            End Set
        End Property

        Public Overridable Property Closure As RkStruct Implements IFunction.Closure
            Get
                Return CType(Me.GetDecideType, IFunction).Closure
            End Get
            Set(value As RkStruct)

                CType(Me.GetDecideType, IFunction).Closure = value
            End Set
        End Property

        Public Overridable ReadOnly Property IsAnonymous As Boolean Implements IFunction.IsAnonymous
            Get
                Return CType(Me.GetDecideType, IFunction).IsAnonymous
            End Get
        End Property

        Public Overridable ReadOnly Property Apply As List(Of IType) Implements IApply.Apply
            Get
                Return CType(Me.GetDecideType, IApply).Apply
            End Get
        End Property

        Public Overridable Sub Add(type As IType)

            If TypeOf type Is RkUnionType Then

                Me.Add(CType(type, RkUnionType).Types)
            Else

                Me.Add({type})
            End If
        End Sub

        Public Overridable Sub Add(types As IEnumerable(Of IType))

            If types Is Nothing Then Return

            Me.ReturnCache = Nothing
            If Me.Types Is Nothing Then

                Me.Types = types.ToList
            Else

                Me.Types.AddRange(types.Where(Function(x) Me.Types.FindFirstOrNull(Function(a) a.Is(x)) Is Nothing))
            End If
        End Sub

        Public Overridable Function Unique() As Boolean

            Dim count = Me.Types.Count
            Me.Types = Me.Types.UniqueHash.ToList
            Return Me.Types.Count <> count
        End Function

        Public Overridable Function Merge(type As IType) As Boolean

            If TypeOf type Is RkUnionType Then

                Return Me.Merge(CType(type, RkUnionType).Types)
            Else

                Return Me.Merge({type})
            End If
        End Function

        Public Overridable Function Merge(types As IEnumerable(Of IType)) As Boolean

            If types Is Nothing Then Return False

            Me.ReturnCache = Nothing
            If Me.Types Is Nothing Then

                Diagnostics.Debug.Assert(types.ToList.Count > 0, "types is empty")
                Dim xs = types.UniqueHash.ToList
                If xs.Count = 0 Then Return False
                Me.Types = xs
                Return True

            ElseIf Me.UnionType = UnionTypes.Addtion Then

                Dim adds = types.Where(Function(x) Me.Types.FindFirstOrNull(Function(a) x.Is(a)) Is Nothing).ToList
                If adds.Count = 0 Then Return False
                Me.Types.AddRange(adds)
                Return True

            ElseIf Me.Types.Count > 0 AndAlso Me.UnionType = UnionTypes.Subtraction Then

                Dim before = Me.Types.Count
                Dim after = Me.Types.Merge(types, Function(a, b) a.Is(b)).ToList
                Dim gens = after.Split(Function(x) TypeOf x Is RkGenericEntry)
                If gens.Item2.Count > 0 Then

                    Dim invert = types.Where(Function(a) Not gens.Item1.Or(Function(b) a.Is(b)))
                    after = gens.Item1.Join(invert).ToList
                End If
                Diagnostics.Debug.Assert(after.Count > 0, "types is empty")
                Me.Types = after
                Return before <> Me.Types.Count
            End If

            Return False
        End Function

        Public Overridable Function GetDecideType() As IType

            If Me.HasIndefinite Then Throw New Exception("operation is failed")
            Return Me.Types(0)
        End Function

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Return Me.GetDecideType.GetValue(name)
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            If Me.Types Is Nothing Then Return True

            If TypeOf t Is RkUnionType Then

                Dim union = CType(t, RkUnionType)
                If union.Types Is Nothing Then Return True
                Return union.Types.Or(Function(x) Me.Is(x))
            Else

                Return Me.Types.Or(Function(x) x.Is(t))
            End If
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Return Me.GetDecideType.DefineGeneric(name)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Return Me.GetDecideType.FixedGeneric(values)
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Return Me.GetDecideType.FixedGeneric(values)
        End Function

        Public Overridable Function TypeToApply(value As IType) As IType() Implements IType.TypeToApply

            Return Me.GetDecideType.TypeToApply(value)
        End Function

        Public Overridable Function WhereFunction(target As IScope, ParamArray args() As IType) As Boolean Implements IFunction.WhereFunction

            Return CType(Me.GetDecideType, IFunction).WhereFunction(target, args)
        End Function

        Public Overridable Function ApplyFunction(scope As IScope, ParamArray args() As IType) As IFunction Implements IFunction.ApplyFunction

            Return CType(Me.GetDecideType, IFunction).ApplyFunction(scope, args)
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.Types IsNot Nothing AndAlso Me.Types.Or(Function(x) x.HasGeneric)
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Return CType(Me.MemberwiseClone, RkUnionType)
        End Function

        Public Overridable Function GetBaseFunctions() As List(Of IFunction) Implements IFunction.GetBaseFunctions

            Return CType(Me.GetDecideType, IFunction).GetBaseFunctions
        End Function

        Public Overridable Function CreateCall(ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCall

            Return CType(Me.GetDecideType, IFunction).CreateCall(args)
        End Function

        Public Overridable Function CreateCallReturn(return_ As OpValue, ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCallReturn

            Return CType(Me.GetDecideType, IFunction).CreateCallReturn(return_, args)
        End Function

        Public Overridable Function CreateManglingName() As String Implements IFunction.CreateManglingName

            Return CType(Me.GetDecideType, IFunction).CreateManglingName
        End Function

        Public Overridable Function HasIndefinite() As Boolean Implements IType.HasIndefinite

            Return Me.Types Is Nothing OrElse Me.Types.Count <> 1
        End Function

        Public Overridable Function HasEmpty() As Boolean

            Return Me.Types Is Nothing OrElse Me.Types.Count = 0
        End Function

        Public Overridable Function Clone(conv As Func(Of INode, INode)) As Node.ICloneable Implements Node.ICloneable.Clone

            Dim copy = CType(Me.MemberwiseClone, RkUnionType)
            copy.Types?.By(Of IFunction).Each(Sub(x) If x.FunctionNode IsNot Nothing Then x.FunctionNode = CType(conv(x.FunctionNode), FunctionNode))
            Return copy
        End Function

        Public Overrides Function ToString() As String

            If Me.Types Is Nothing Then Return "_"
            If Me.HasIndefinite Then Return String.Join("|", Me.Types)
            Return Me.GetDecideType.Name
        End Function
    End Class

End Namespace
