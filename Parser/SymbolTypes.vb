
Namespace Parser

    Public Enum SymbolTypes As Integer

        ''' <summary>'('</summary>
        __x28 = 0

        ''' <summary>')'</summary>
        __x29

        ''' <summary>','</summary>
        __x2C

        ''' <summary>'.'</summary>
        __x2E

        ''' <summary>':'</summary>
        __x3A

        ''' <summary>'?'</summary>
        __x3F

        ''' <summary>'['</summary>
        __x5B

        ''' <summary>']'</summary>
        __x5D

        ''' <summary>'{'</summary>
        __x7B

        ''' <summary>'}'</summary>
        __x7D

        ''' <summary>AND2</summary>
        AND2

        ''' <summary>ARROW</summary>
        ARROW

        ''' <summary>ATVAR</summary>
        ATVAR

        ''' <summary>BEGIN</summary>
        BEGIN

        ''' <summary>CLASS</summary>
        [CLASS]

        ''' <summary>ELSE</summary>
        [ELSE]

        ''' <summary>END</summary>
        [END]

        ''' <summary>EOL</summary>
        EOL

        ''' <summary>EQ</summary>
        EQ

        ''' <summary>FALSE</summary>
        [FALSE]

        ''' <summary>GT</summary>
        GT

        ''' <summary>IF</summary>
        [IF]

        ''' <summary>IGNORE</summary>
        IGNORE

        ''' <summary>LET</summary>
        [LET]

        ''' <summary>LT</summary>
        LT

        ''' <summary>NULL</summary>
        NULL

        ''' <summary>NUM</summary>
        NUM

        ''' <summary>OPE</summary>
        OPE

        ''' <summary>OR</summary>
        [OR]

        ''' <summary>OR2</summary>
        OR2

        ''' <summary>STR</summary>
        STR

        ''' <summary>STRUCT</summary>
        STRUCT

        ''' <summary>SUB</summary>
        [SUB]

        ''' <summary>SWITCH</summary>
        SWITCH

        ''' <summary>TRUE</summary>
        [TRUE]

        ''' <summary>UNARY</summary>
        UNARY

        ''' <summary>UNION</summary>
        UNION

        ''' <summary>USE</summary>
        USE

        ''' <summary>VAR</summary>
        VAR

        ''' <summary>$END</summary>
        _END

        ''' <summary>and</summary>
        [and]

        ''' <summary>argn</summary>
        argn

        ''' <summary>args</summary>
        args

        ''' <summary>array_pattern</summary>
        array_pattern

        ''' <summary>atvar</summary>
        atvar_1

        ''' <summary>atvarn</summary>
        atvarn

        ''' <summary>begin</summary>
        begin_1

        ''' <summary>block</summary>
        block

        ''' <summary>bool</summary>
        bool

        ''' <summary>call</summary>
        [call]

        ''' <summary>case</summary>
        [case]

        ''' <summary>case_block</summary>
        case_block

        ''' <summary>case_expr</summary>
        case_expr

        ''' <summary>casen</summary>
        casen

        ''' <summary>cexpr</summary>
        cexpr

        ''' <summary>class</summary>
        class_1

        ''' <summary>class_block</summary>
        class_block

        ''' <summary>cond</summary>
        cond

        ''' <summary>condn</summary>
        condn

        ''' <summary>decla</summary>
        decla

        ''' <summary>define</summary>
        define

        ''' <summary>elseif</summary>
        [elseif]

        ''' <summary>expr</summary>
        expr

        ''' <summary>extra</summary>
        extra

        ''' <summary>fn</summary>
        fn

        ''' <summary>fvar</summary>
        fvar

        ''' <summary>if</summary>
        if_1

        ''' <summary>ifthen</summary>
        ifthen

        ''' <summary>lambda</summary>
        lambda

        ''' <summary>lambda_arg</summary>
        lambda_arg

        ''' <summary>lambda_argn</summary>
        lambda_argn

        ''' <summary>lambda_args</summary>
        lambda_args

        ''' <summary>lambda_func</summary>
        lambda_func

        ''' <summary>let</summary>
        let_1

        ''' <summary>line</summary>
        line

        ''' <summary>list</summary>
        list

        ''' <summary>list2n</summary>
        list2n

        ''' <summary>listn</summary>
        listn

        ''' <summary>namespace</summary>
        [namespace]

        ''' <summary>nope</summary>
        nope

        ''' <summary>nsvar</summary>
        nsvar

        ''' <summary>nsvarn</summary>
        nsvarn

        ''' <summary>null</summary>
        null_1

        ''' <summary>num</summary>
        num_1

        ''' <summary>ope</summary>
        ope_1

        ''' <summary>or</summary>
        or_1

        ''' <summary>pattern</summary>
        pattern

        ''' <summary>patternn</summary>
        patternn

        ''' <summary>patterns</summary>
        patterns

        ''' <summary>program_begin</summary>
        program_begin

        ''' <summary>start</summary>
        start

        ''' <summary>stmt</summary>
        stmt

        ''' <summary>str</summary>
        str_1

        ''' <summary>struct</summary>
        struct_1

        ''' <summary>struct_begin</summary>
        struct_begin

        ''' <summary>struct_block</summary>
        struct_block

        ''' <summary>sub</summary>
        sub_1

        ''' <summary>sub_begin</summary>
        sub_begin

        ''' <summary>sub_block</summary>
        sub_block

        ''' <summary>switch</summary>
        switch_1

        ''' <summary>tuplevar</summary>
        tuplevar

        ''' <summary>tuplevar2n</summary>
        tuplevar2n

        ''' <summary>tupple_pattern</summary>
        tupple_pattern

        ''' <summary>type</summary>
        type

        ''' <summary>type2n</summary>
        type2n

        ''' <summary>typen</summary>
        typen

        ''' <summary>typeor</summary>
        typeor

        ''' <summary>types</summary>
        types

        ''' <summary>typev</summary>
        typev

        ''' <summary>typex</summary>
        typex

        ''' <summary>union</summary>
        union_1

        ''' <summary>unionn</summary>
        unionn

        ''' <summary>use</summary>
        use_1

        ''' <summary>uses</summary>
        uses

        ''' <summary>var</summary>
        var_1

        ''' <summary>varx</summary>
        varx

        ''' <summary>void</summary>
        void

        ''' <summary>where</summary>
        [where]

        ''' <summary>$ACCEPT</summary>
        _ACCEPT

    End Enum

End Namespace
