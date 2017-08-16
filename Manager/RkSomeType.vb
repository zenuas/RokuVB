Imports System
Imports System.Collections.Generic
Imports Roku.IntermediateCode
Imports Roku.Node
Imports Roku.Operator
Imports Roku.Util.ArrayExtension


Namespace Manager

    Public Class RkSomeType
        Implements IFunction

        Public Overridable Property Types As List(Of IType)
        Public Overridable Property ReturnCache As RkSomeType

        Public Sub New()

        End Sub

        Public Sub New(xs As IEnumerable(Of IType))

            Me.Merge(xs)
        End Sub

        Public Overridable Property Name As String Implements IEntry.Name
            Get
                Return Me.GetDecideType.Name
            End Get
            Set(value As String)

                Me.GetDecideType.Name = value
            End Set
        End Property

        Public Overridable Property [Namespace] As RkNamespace Implements IType.Namespace
            Get
                Return Me.GetDecideType.Namespace
            End Get
            Set(value As RkNamespace)

                Me.GetDecideType.Namespace = value
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
                If Me.Types.Count = 1 Then Return Me.Types(0)

                If Me.ReturnCache Is Nothing Then Me.ReturnCache = New RkSomeType(Me.Types.Where(Of RkFunction)(Function(x) x.Return IsNot Nothing).Map(Function(x) x.Return))
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
                Return CType(Me.GetDecideType, IFunction).Apply
            End Get
        End Property

        Public Overridable Sub Merge(type As IType)

            Me.Merge({type})
        End Sub

        Public Overridable Sub Merge(types As IEnumerable(Of IType))

            Me.ReturnCache = Nothing
            If Me.Types Is Nothing Then

                Me.Types = types.ToList

            ElseIf Me.Types.Count > 0 Then

                Me.Types = Me.Types.Merge(types).ToList
            End If
        End Sub

        Public Overridable Function GetDecideType() As IType

            If Me.Indefinite Then Throw New Exception("operation is failed")
            Return Me.Types(0)
        End Function

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Return Me.GetDecideType.GetValue(name)
        End Function

        Public Overridable Function [Is](t As IType) As Boolean Implements IType.Is

            If Me.Types Is Nothing Then Return True

            If TypeOf t Is RkSomeType Then

                Return CType(t, RkSomeType).Types.Or(Function(x) Me.Is(x))
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

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Return Me.GetDecideType.HasGeneric
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Return Me.GetDecideType.CloneGeneric
        End Function

        Public Overridable Function GetBaseFunctions() As List(Of IFunction) Implements IFunction.GetBaseFunctions

            Return CType(Me.GetDecideType, IFunction).GetBaseFunctions
        End Function

        Public Overridable Function CreateCall(self As OpValue, ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCall

            Return CType(Me.GetDecideType, IFunction).CreateCall(self, args)
        End Function

        Public Overridable Function CreateCallReturn(self As OpValue, return_ As OpValue, ParamArray args() As OpValue) As InCode0() Implements IFunction.CreateCallReturn

            Return CType(Me.GetDecideType, IFunction).CreateCallReturn(self, return_, args)
        End Function

        Public Overridable Function CreateManglingName() As String Implements IFunction.CreateManglingName

            Return CType(Me.GetDecideType, IFunction).CreateManglingName
        End Function

        Public Overridable Function Indefinite() As Boolean Implements IType.Indefinite

            Return Me.Types Is Nothing OrElse Me.Types.Count <> 1
        End Function

        Public Overrides Function ToString() As String

            If Me.Indefinite Then Return "_"
            Return Me.GetDecideType.Name
        End Function
    End Class

End Namespace
