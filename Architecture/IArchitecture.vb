Imports Roku.Manager


Namespace Architecture

    Public Interface IArchitecture

        Sub Assemble(ns As SystemLirary)
        Sub Optimize()
        Sub Emit(path As String)

    End Interface

End Namespace
