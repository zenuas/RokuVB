
%{
Imports Roku.Node
%}

%default INode
%define YYROOTNAMESPACE Roku
%define YYNAMESPACE     Compiler

%type<BlockNode>        block
%type<LetNode>          let
%type<IfNode>           if ifthen elseif
%type<IEvaluableNode>   expr
%type<IEvaluableNode>   call
%type<ListNode>         list
%type<VariableNode>     var varx
%type<NumericNode>      num
%type<StringNode>       str

%left  ELSE
%left  OPE
%left  '?'
%right '(' '['

%%

program : void
        | program stmt


########## statement ##########
stmt : line
     | sub
     | if
     | block

block : BEGIN program END


########## expr ##########
line : expr EOL
     | let  EOL

expr : var
     | str
     | num
     | call
     | '(' expr ')'      {$$ = Me.CreateExpressionNode($2, "()")}
#     | OPE expr          {$$ = Me.CreateExpressionNode($2, $1.Name)}
     | expr OPE expr     {$$ = Me.CreateExpressionNode($1, $2.Name, $3)}
     | expr '[' expr ']' {$$ = Me.CreateExpressionNode($1, "[]", $3)}
     | expr '?' expr ':' expr

call : expr list         {$$ = New FunctionCallNode($1, $2.List.ToArray)}

list : expr              {$$ = Me.CreateListNode($1)}
     | list expr         {$1.List.Add($2) : $$ = $1}


########## let ##########
let : LET var EQ expr    {$$ = Me.CreateLetNode($2, $4)}


########## sub ##########
sub   : SUB var '(' args ')' type EOL block
args  : void
      | argn
argn  : decla
      | argn decla
decla : var ':' type
type  : var
      | '[' type ']'


########## if ##########
if     : ifthen
       | elseif
       | if ELSE EOL block  {$1.Else = $4 : $$ = $1}
ifthen : IF expr EOL block  {$$ = Me.CreateIfNode($2, $4)}
elseif : ifthen ELSE ifthen {$1.Else = $3 : $$ = $1}
       | elseif ELSE ifthen {$1.Else = $3 : $$ = $1}

########## other ##########
var   : VAR     {$$ = Me.CreateVariableNode($1)}
varx  : var
      | SUB     {$$ = Me.CreateVariableNode($1)}
      | IF      {$$ = Me.CreateVariableNode($1)}
      | ELSE    {$$ = Me.CreateVariableNode($1)}
      | LET     {$$ = Me.CreateVariableNode($1)}
num   : NUM     {$$ = $1}
str   | STR     {$$ = New StringNode($1)}
      | str STR {$1.Append($2.Name) : $$ = $1}

void : {$$ = Nothing}

