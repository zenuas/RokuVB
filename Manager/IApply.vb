Imports System.Collections.Generic


Namespace Manager

    Public Interface IApply

        ReadOnly Property Apply As List(Of IType)
        ReadOnly Property Generics As List(Of RkGenericEntry)

    End Interface

End Namespace
