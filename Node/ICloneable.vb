Imports System


Namespace Node

    Public Interface ICloneable

        Function Clone(conv As Func(Of INode, INode)) As ICloneable

    End Interface

End Namespace
