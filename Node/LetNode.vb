Imports System
Imports System.Collections.Generic
Imports Roku.Manager


Namespace Node

    Public Class LetNode
        Inherits BaseNode
        Implements IEvaluableNode


        Public Overridable Property Receiver As IEvaluableNode
        Public Overridable Property Var As VariableNode
        Public Overridable Property [Declare] As TypeNode
        Public Overridable Property Expression As IEvaluableNode
        Public Overridable Property Type As IType Implements IEvaluableNode.Type
        Public Overridable Property NameBinding As Boolean = False
    End Class

End Namespace
