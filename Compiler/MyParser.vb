
Imports Roku.Node


Imports System
Imports Roku.Compiler

Namespace Compiler

    Public Class MyParser
        Inherits Parser(Of INode)

        Private Shared ReadOnly tables_(,) As Integer = { _
                {-1, 0, 0, 0, 0, 0, -1, 0, -1, 0, 0, -1, -1, -1, 0, -1, -1, -1, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 1, 0, 0, -1, 0, 0, 0, 0, 0, 0, -1}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -2}, _
                {-3, 0, 0, 0, 0, 0, -3, 0, -3, 0, 0, -3, -3, -3, 0, -3, -3, -3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 65, -3, 0, 0, 0, 0, 5, 0, -3}, _
                {-4, 0, 0, 0, 0, 0, -4, 0, -4, 0, 0, -4, -4, -4, 0, -4, -4, -4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -4, 0, 0, 0, 0, 0, 0, -4}, _
                {-5, 0, 0, 0, 0, 0, -5, 0, -5, 0, 0, -5, -5, -5, 0, -5, -5, -5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -5, 0, 0, 0, 0, 0, 0, -5}, _
                {-6, 0, 0, 0, 0, 0, -6, 0, -6, 0, 0, -6, -6, -6, 0, -6, -6, -6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -6, 0, 0, 0, 0, 0, 0, -6}, _
                {-7, 0, 0, 0, 0, 0, -7, 0, -7, 0, 0, -7, -7, -7, 0, -7, -7, -7, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -7, 0, 0, 0, 0, 0, 0, -7}, _
                {-8, 0, 0, 0, 0, 0, -8, 0, -8, 0, 0, -8, -8, -8, 0, -8, -8, -8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -8, 0, 0, 0, 0, 0, 0, -8}, _
                {-9, 0, 0, 0, 0, 0, -9, 0, -9, 0, 0, -9, -9, -9, 0, -9, -9, -9, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 11, 0, 0, 0, -9, 0, 0, 0, 0, 0, 0, -9}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {-10, 0, 0, 0, 0, 0, -10, -10, -10, 0, 0, -10, -10, -10, 0, -10, -10, -10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -10, 0, 0, 0, 0, 0, 0, -10}, _
                {-11, 0, 0, 0, 0, 0, -11, 0, -11, 0, 0, -11, -11, -11, 0, -11, -11, -11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -11, 0, 0, 0, 0, 0, 0, -11}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {-12, 0, 0, 0, 0, 0, -12, 0, -12, 0, 0, -12, -12, -12, 0, -12, -12, -12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -12, 0, 0, 0, 0, 0, 0, -12}, _
                {-13, -13, -13, -13, -13, -13, 0, 0, 0, -13, 0, 0, 0, -13, -13, -13, 0, -13, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -13, 0, 0, 0, 0, 0, 0, 0}, _
                {-14, -14, -14, -14, -14, -14, 0, 0, 0, -14, 0, 0, 0, -14, -14, -14, 0, -14, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -14, 0, 0, 0, 0, 0, 0, 0}, _
                {-15, -15, -15, -15, -15, -15, 0, 0, 0, -15, 0, 0, 0, -15, -15, -15, 0, -15, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -15, 0, 0, 0, 0, 0, 0, 0}, _
                {19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 71, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {-16, -16, -16, -16, -16, -16, 0, 0, 0, -16, 0, 0, 0, -16, -16, -16, 0, -16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -16, 0, 0, 0, 0, 0, 0, 0}, _
                {19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 72, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 73, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {-17, -17, -17, -17, -17, -17, 0, 0, 0, -17, 0, 0, 0, -17, -17, -17, 0, -17, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -17, 0, 0, 0, 0, 0, 0, 0}, _
                {19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 74, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 75, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 59, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 27, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 28, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 79, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 59, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 30, 0, 0, 0}, _
                {31, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, -18, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 59, 80, 32, 0, 0, 0, 38, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 40, 37, 0, 0}, _
                {0, 33, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 44, 0, 0, 0, 0, -19, 0, 0, 0, 0, 0, 0, 0, 59, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 48, 34, 43, 47, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 35, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 36, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {-20, 0, 0, 0, 0, 0, -20, 0, -20, 0, 0, -20, -20, -20, 0, -20, -20, -20, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -20, 0, 0, 0, 0, 0, 0, -20}, _
                {0, -21, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, -22, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -22, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, -23, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -23, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 44, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 59, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 42, 0, 43, 0, 0, 0}, _
                {0, -24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -24, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, -25, 0, 0, 0, -25, 0, 0, 0, -25, 0, 0, 0, 0, 0, 0, 0, -25, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 44, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 59, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 45, 0, 43, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 46, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, -26, 0, 0, 0, -26, 0, 0, 0, -26, 0, 0, 0, 0, 0, 0, 0, -26, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, -27, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, -28, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 51, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {-29, 0, 0, 0, 0, 0, -29, -29, -29, 0, 0, -29, -29, -29, 0, -29, -29, -29, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -29, 0, 0, 0, 0, 0, 0, -29}, _
                {19, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 60, 0, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 81, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 54, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {-30, 0, 0, 0, 0, 0, -30, -30, -30, 0, 0, -30, -30, -30, 0, -30, -30, -30, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -30, 0, 0, 0, 0, 0, 0, -30}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 52, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 56, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {-31, 0, 0, 0, 0, 0, -31, -31, -31, 0, 0, -31, -31, -31, 0, -31, -31, -31, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -31, 0, 0, 0, 0, 0, 0, -31}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 52, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 58, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}, _
                {-32, 0, 0, 0, 0, 0, -32, -32, -32, 0, 0, -32, -32, -32, 0, -32, -32, -32, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -32, 0, 0, 0, 0, 0, 0, -32}, _
                {-33, -33, -33, -33, -33, -33, 0, 0, 0, -33, -33, 0, 0, -33, -33, -33, 0, -33, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -33, 0, 0, 0, 0, 0, 0, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -34, 0, 0, 0, 0, 0, 61, 0}, _
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 0, 0, 0, 0, 0, 0, 0}, _
                {-35, -35, -35, -35, -35, -35, 0, 0, 0, -35, 0, 0, 0, -35, -35, -35, 0, -35, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -35, 0, 0, 0, 0, 0, 0, 0}, _
                {-36, -36, -36, -36, -36, -36, 0, 0, 0, -36, 0, 0, 0, -36, -36, -36, 0, -36, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -36, 0, 0, 0, 0, 0, 0, 0}, _
                {-37, -37, -37, -37, -37, -37, 0, 0, 0, -37, 0, 0, 0, -37, -37, -37, 0, -37, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -37, 0, 0, 0, 0, 0, 0, 0}, _
                {19, 0, 0, 0, 0, 0, 10, 0, -38, 0, 0, 52, 26, 60, 0, 63, 29, 59, 0, 0, 0, 9, 18, 0, 68, 70, 66, 67, 14, 7, 0, 17, 0, 0, 6, 0, 69, 8, 0, 0, 16, 0, 0, -38}, _
                {-39, 0, 0, 0, 0, 0, -39, 49, -39, 0, 0, -39, -39, -39, 0, -39, -39, -39, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -39, 0, 0, 0, 0, 0, 0, -39}, _
                {-40, 0, 0, 0, 0, 0, -40, 55, -40, 0, 0, -40, -40, -40, 0, -40, -40, -40, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -40, 0, 0, 0, 0, 0, 0, -40}, _
                {-41, 0, 0, 0, 0, 0, -41, 57, -41, 0, 0, -41, -41, -41, 0, -41, -41, -41, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -41, 0, 0, 0, 0, 0, 0, -41}, _
                {-42, -42, -42, -42, -42, -42, 0, 0, 0, -42, 0, 0, 0, -42, -42, 64, 0, -42, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -42, 0, 0, 0, 0, 0, 0, 0}, _
                {19, 0, 0, 24, 22, 0, 0, 0, 0, 13, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, 20, 0, 24, 22, 0, 0, 0, 0, 0, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, -43, -43, 24, 22, -43, 0, 0, 0, -43, 0, 0, 0, 60, -43, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, 0, 0, 24, 22, 23, 0, 0, 0, 0, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, 0, 25, 24, 22, 0, 0, 0, 0, 0, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, -44, -44, -44, 22, -44, 0, 0, 0, -44, 0, 0, 0, 60, -44, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, -45, -45, -45, -45, -45, 0, 0, 0, -45, 0, 0, 0, 60, -45, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 78, 0, 0, 0, 0, 0, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, -46, -46, 24, 22, -46, 0, 0, 0, -46, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, -47, -47, 24, 22, -47, 0, 0, 0, -47, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {19, 0, 0, 24, 22, 0, 0, 0, 0, -48, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0}, _
                {0, -49, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 59, 0, 0, 0, 0, 0, 39, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 40, 0, 0, 0}, _
                {19, 0, 0, 24, 22, 0, 0, 0, 0, 53, 0, 0, 0, 60, 21, 63, 0, 59, 0, 0, 0, 0, 18, 0, 0, 77, 0, 0, 0, 0, 76, 17, 0, 0, 0, 0, 69, 0, 0, 0, 16, 0, 0, 0} _
            }

        Protected Overrides Function CreateTable() As Integer(,)

            Return tables_
        End Function

        Protected Overrides Function RunAction(ByVal yy_no As Integer) As IToken(Of INode)

            Dim yy_token As IToken(Of INode) = Nothing
            Dim yy_value As INode = Nothing

            Select Case yy_no
                Case -1
                    System.Diagnostics.Debug.WriteLine("begin : .")
                    Me.PushScope(New BlockNode)
                    yy_token = Me.DoAction(SymbolTypes.begin_1, 0, yy_value)

                Case -2
                    System.Diagnostics.Debug.WriteLine("start : program .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.start, 1, yy_value)

                Case -3
                    System.Diagnostics.Debug.WriteLine("void : .")
                    yy_value = Nothing
                    yy_token = Me.DoAction(SymbolTypes.void, 0, yy_value)

                Case -4
                    System.Diagnostics.Debug.WriteLine("stmts : void .")
                    yy_value = Me.CurrentScope
                    yy_token = Me.DoAction(SymbolTypes.stmts, 1, yy_value)

                Case -5
                    System.Diagnostics.Debug.WriteLine("stmts : stmts stmt .")
                    CType(Me.GetValue(-2), BlockNode).AddStatement(CType(Me.GetValue(-1), IEvaluableNode)) : yy_value = CType(Me.GetValue(-2), BlockNode)
                    yy_token = Me.DoAction(SymbolTypes.stmts, 2, yy_value)

                Case -6
                    System.Diagnostics.Debug.WriteLine("stmt : line .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.stmt, 1, yy_value)

                Case -7
                    System.Diagnostics.Debug.WriteLine("stmt : sub .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.stmt, 1, yy_value)

                Case -8
                    System.Diagnostics.Debug.WriteLine("stmt : block .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.stmt, 1, yy_value)

                Case -9
                    System.Diagnostics.Debug.WriteLine("begin : .")
                    Me.PushScope(New BlockNode)
                    yy_token = Me.DoAction(SymbolTypes.begin_1, 0, yy_value)

                Case -10
                    System.Diagnostics.Debug.WriteLine("block : BEGIN program END .")
                    yy_value = (Me.GetValue(-2))
                    yy_token = Me.DoAction(SymbolTypes.block, 3, yy_value)

                Case -11
                    System.Diagnostics.Debug.WriteLine("line : expr EOL .")
                    yy_value = Me.DefaultAction(2)
                    yy_token = Me.DoAction(SymbolTypes.line, 2, yy_value)

                Case -12
                    System.Diagnostics.Debug.WriteLine("line : let EOL .")
                    yy_value = Me.DefaultAction(2)
                    yy_token = Me.DoAction(SymbolTypes.line, 2, yy_value)

                Case -13
                    System.Diagnostics.Debug.WriteLine("expr : var .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.expr, 1, yy_value)

                Case -14
                    System.Diagnostics.Debug.WriteLine("expr : num .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.expr, 1, yy_value)

                Case -15
                    System.Diagnostics.Debug.WriteLine("expr : call .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.expr, 1, yy_value)

                Case -16
                    System.Diagnostics.Debug.WriteLine("expr : '(' expr ')' .")
                    yy_value = Me.CreateExpressionNode(CType(Me.GetValue(-2), IEvaluableNode), "()")
                    yy_token = Me.DoAction(SymbolTypes.expr, 3, yy_value)

                Case -17
                    System.Diagnostics.Debug.WriteLine("expr : expr '[' expr ']' .")
                    yy_value = Me.CreateExpressionNode(CType(Me.GetValue(-4), IEvaluableNode), "[]", CType(Me.GetValue(-2), IEvaluableNode))
                    yy_token = Me.DoAction(SymbolTypes.expr, 4, yy_value)

                Case -18
                    System.Diagnostics.Debug.WriteLine("void : .")
                    yy_value = Nothing
                    yy_token = Me.DoAction(SymbolTypes.void, 0, yy_value)

                Case -19
                    System.Diagnostics.Debug.WriteLine("void : .")
                    yy_value = Nothing
                    yy_token = Me.DoAction(SymbolTypes.void, 0, yy_value)

                Case -20
                    System.Diagnostics.Debug.WriteLine("sub : SUB var '(' args ')' typex EOL block .")
                    yy_value = Me.DefaultAction(8)
                    yy_token = Me.DoAction(SymbolTypes.sub_1, 8, yy_value)

                Case -21
                    System.Diagnostics.Debug.WriteLine("args : void .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.args, 1, yy_value)

                Case -22
                    System.Diagnostics.Debug.WriteLine("argn : decla .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.argn, 1, yy_value)

                Case -23
                    System.Diagnostics.Debug.WriteLine("argn : argn decla .")
                    yy_value = Me.DefaultAction(2)
                    yy_token = Me.DoAction(SymbolTypes.argn, 2, yy_value)

                Case -24
                    System.Diagnostics.Debug.WriteLine("decla : var ':' type .")
                    yy_value = Me.DefaultAction(3)
                    yy_token = Me.DoAction(SymbolTypes.decla, 3, yy_value)

                Case -25
                    System.Diagnostics.Debug.WriteLine("type : var .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.type, 1, yy_value)

                Case -26
                    System.Diagnostics.Debug.WriteLine("type : '[' type ']' .")
                    yy_value = Me.DefaultAction(3)
                    yy_token = Me.DoAction(SymbolTypes.type, 3, yy_value)

                Case -27
                    System.Diagnostics.Debug.WriteLine("typex : void .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.typex, 1, yy_value)

                Case -28
                    System.Diagnostics.Debug.WriteLine("typex : type .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.typex, 1, yy_value)

                Case -29
                    System.Diagnostics.Debug.WriteLine("if : if ELSE EOL block .")
                    CType(Me.GetValue(-4), IfNode).Else = CType(Me.GetValue(-1), BlockNode) : yy_value = CType(Me.GetValue(-4), IfNode)
                    yy_token = Me.DoAction(SymbolTypes.if_1, 4, yy_value)

                Case -30
                    System.Diagnostics.Debug.WriteLine("ifthen : IF expr EOL block .")
                    yy_value = Me.CreateIfNode(CType(Me.GetValue(-3), IEvaluableNode), CType(Me.GetValue(-1), BlockNode))
                    yy_token = Me.DoAction(SymbolTypes.ifthen, 4, yy_value)

                Case -31
                    System.Diagnostics.Debug.WriteLine("elseif : ifthen ELSE ifthen .")
                    CType(Me.GetValue(-3), IfNode).Else = CType(Me.GetValue(-1), IfNode) : yy_value = CType(Me.GetValue(-3), IfNode)
                    yy_token = Me.DoAction(SymbolTypes.[elseif], 3, yy_value)

                Case -32
                    System.Diagnostics.Debug.WriteLine("elseif : elseif ELSE ifthen .")
                    CType(Me.GetValue(-3), IfNode).Else = CType(Me.GetValue(-1), IfNode) : yy_value = CType(Me.GetValue(-3), IfNode)
                    yy_token = Me.DoAction(SymbolTypes.[elseif], 3, yy_value)

                Case -33
                    System.Diagnostics.Debug.WriteLine("var : VAR .")
                    yy_value = Me.CreateVariableNode(CType(Me.GetToken(-1), Token))
                    yy_token = Me.DoAction(SymbolTypes.var_1, 1, yy_value)

                Case -34
                    System.Diagnostics.Debug.WriteLine("$1 : .")
                    yy_value = Nothing
                    yy_token = Me.DoAction(SymbolTypes.__x241, 0, yy_value)

                Case -35
                    System.Diagnostics.Debug.WriteLine("num : NUM $1 str .")
                    yy_value = Me.DefaultAction(3)
                    yy_token = Me.DoAction(SymbolTypes.num_1, 3, yy_value)

                Case -36
                    System.Diagnostics.Debug.WriteLine("num : STR .")
                    yy_value = New StringNode(CType(Me.GetToken(-1), Token))
                    yy_token = Me.DoAction(SymbolTypes.num_1, 1, yy_value)

                Case -37
                    System.Diagnostics.Debug.WriteLine("num : str STR .")
                    CType(Me.GetValue(-2), StringNode).Append(CType(Me.GetToken(-1), Token).Name) : yy_value = CType(Me.GetValue(-2), StringNode)
                    yy_token = Me.DoAction(SymbolTypes.num_1, 2, yy_value)

                Case -38
                    System.Diagnostics.Debug.WriteLine("program : begin stmts .")
                    yy_value = Me.PopScope
                    yy_token = Me.DoAction(SymbolTypes.program, 2, yy_value)

                Case -39
                    System.Diagnostics.Debug.WriteLine("stmt : if .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.stmt, 1, yy_value)

                Case -40
                    System.Diagnostics.Debug.WriteLine("if : ifthen .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.if_1, 1, yy_value)

                Case -41
                    System.Diagnostics.Debug.WriteLine("if : elseif .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.if_1, 1, yy_value)

                Case -42
                    System.Diagnostics.Debug.WriteLine("expr : str .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.expr, 1, yy_value)

                Case -43
                    System.Diagnostics.Debug.WriteLine("expr : expr OPE expr .")
                    yy_value = Me.CreateExpressionNode(CType(Me.GetValue(-3), IEvaluableNode), CType(Me.GetToken(-2), Token).Name, CType(Me.GetValue(-1), IEvaluableNode))
                    yy_token = Me.DoAction(SymbolTypes.expr, 3, yy_value)

                Case -44
                    System.Diagnostics.Debug.WriteLine("expr : expr '?' expr ':' expr .")
                    yy_value = Me.DefaultAction(5)
                    yy_token = Me.DoAction(SymbolTypes.expr, 5, yy_value)

                Case -45
                    System.Diagnostics.Debug.WriteLine("call : expr list .")
                    yy_value = New FunctionCallNode(CType(Me.GetValue(-2), IEvaluableNode), CType(Me.GetValue(-1), ListNode).List.ToArray)
                    yy_token = Me.DoAction(SymbolTypes.[call], 2, yy_value)

                Case -46
                    System.Diagnostics.Debug.WriteLine("list : expr .")
                    yy_value = Me.CreateListNode(CType(Me.GetValue(-1), IEvaluableNode))
                    yy_token = Me.DoAction(SymbolTypes.list, 1, yy_value)

                Case -47
                    System.Diagnostics.Debug.WriteLine("list : list expr .")
                    CType(Me.GetValue(-2), ListNode).List.Add(CType(Me.GetValue(-1), IEvaluableNode)) : yy_value = CType(Me.GetValue(-2), ListNode)
                    yy_token = Me.DoAction(SymbolTypes.list, 2, yy_value)

                Case -48
                    System.Diagnostics.Debug.WriteLine("let : LET var EQ expr .")
                    yy_value = Me.CreateLetNode(CType(Me.GetValue(-3), VariableNode), CType(Me.GetValue(-1), IEvaluableNode))
                    yy_token = Me.DoAction(SymbolTypes.let_1, 4, yy_value)

                Case -49
                    System.Diagnostics.Debug.WriteLine("args : argn .")
                    yy_value = Me.DefaultAction(1)
                    yy_token = Me.DoAction(SymbolTypes.args, 1, yy_value)

                Case Else
                    Throw New InvalidProgramException
            End Select

            Return yy_token
        End Function

        Protected Overridable Overloads Function DoAction( _
                ByVal type As SymbolTypes, _
                ByVal length As Integer, _
                ByVal value As INode _
            ) As IToken(Of INode)

            Return Me.DoAction(New Token(type), length, value)
        End Function

        Protected Overrides Sub OnError(ByVal lex As Compiler.Lexer(Of INode))

            Throw New SyntaxErrorException(lex.LineNumber, lex.LineColumn, "syntax error")
        End Sub
    End Class

End Namespace

