﻿Imports System.Collections.Generic
Imports Roku.Node
Imports Roku.Operator
Imports Roku.IntermediateCode


Namespace Manager

    Public Interface IFunction
        Inherits IType, IApply, IClosure, ICloneable

        ReadOnly Property Arguments As List(Of NamedValue)
        Property [Return] As IType
        ReadOnly Property Body As List(Of InCode0)
        ReadOnly Property Generics As List(Of RkGenericEntry)
        Property GenericBase As RkFunction
        Property FunctionNode As FunctionNode
        ReadOnly Property IsAnonymous As Boolean
        Function ArgumentsToApply(ParamArray args() As IType) As IType()
        Function ApplyFunction(ParamArray args() As IType) As IFunction
        Function GetBaseFunctions() As List(Of IFunction)
        Function CreateCall(ParamArray args() As OpValue) As InCode0()
        Function CreateCallReturn(return_ As OpValue, ParamArray args() As OpValue) As InCode0()
        Function CreateManglingName() As String

    End Interface

End Namespace
