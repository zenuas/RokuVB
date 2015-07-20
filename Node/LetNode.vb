Imports System.Collections.Generic


Namespace Node

    Public Class LetNode
        Inherits BaseNode
        Implements IRunableNode


        Public Overridable Property Var As VariableNode
        Public Overridable Property Right As IEvaluableNode Implements IRunableNode.Expression
        Public Overridable Property [Next] As IRunableNode Implements IRunableNode.Next
    End Class

End Namespace
