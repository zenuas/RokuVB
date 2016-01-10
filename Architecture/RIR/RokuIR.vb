Imports Roku.Manager


Namespace Architecture.RIR

    <ArchitectureName("RIR")>
    Public Class RokuIR
        Implements IArchitecture

        Public Overridable Property Root As RkNamespace

        Public Overridable Sub Assemble(ns As RkNamespace) Implements IArchitecture.Assemble

            Me.Root = ns
        End Sub

        Public Overridable Sub Optimize() Implements IArchitecture.Optimize

            'Throw New NotImplementedException()
        End Sub

        Public Overridable Sub Emit(path As String) Implements IArchitecture.Emit

            Using out As New SourceWriter(path)

                out.WriteLine("# roku-ir")
                out.WriteLine()

                For Each fs In Me.Root.Functions

                    For Each f In fs.Value

                        If f.HasGeneric OrElse TypeOf f Is RkNativeFunction Then Continue For

                        out.WriteLine(f.ToString)
                    Next
                Next
            End Using
        End Sub
    End Class

End Namespace
