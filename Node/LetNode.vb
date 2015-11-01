Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class LetNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property Var As VariableNode
        Public Overridable Property Expression As IEvaluableNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
    End Class

End Namespace
