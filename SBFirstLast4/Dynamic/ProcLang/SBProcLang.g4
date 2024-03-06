grammar SBProcLang;

script: statement* EOF ;

statement: assignment 
         | wideAssignment 
         | internalAssignment
         | memberAssignment
         | variableDeletion
         | print 
         | if_else_stat 
         | switch_stat
         | do_while_stat
         | while_stat 
         | do_until_stat
         | until_stat 
         | for_stat
         | foreach_stat
         | raise_stat
         | whenany_stat
         | whenever_stat
         | implicit_throw_stat
         | throw_stat
         | try_catch_stat
         | break_stat
         | continue_stat
         | redo_stat
         | retry_stat
         | return_stat
         | clear_stat
         | delay_stat
         | expr_stat
         | empty_stat
         ;

stat_block: '{' statement* '}' | statement ;

if_else_stat: 'if' '(' expr ')' stat_block ('else' 'if' '(' expr ')' stat_block)* ('else' stat_block)? ;

switch_stat: 'switch' '(' expr ')' '{' ( 'case'? factor ':' stat_block )* ( ( 'default' | '_' ) ':' default_stat=stat_block )? '}' ;

do_while_stat: 'do' stat_block 'while' '(' expr ')' ';' ;

while_stat: 'while' '(' expr ')' stat_block ;

do_until_stat: 'do' stat_block 'until' '(' expr ')' ';' ;

until_stat: 'until' '(' expr ')' stat_block ;

for_stat: 'for' '(' init=expr? ';' cond=expr? ';' update=expr? ')' stat_block ;

foreach_stat: 'foreach' '(' ( WideID | InternalID ) 'in' expr ')' stat_block ;

raise_stat: 'raise' ID? ';' ;

whenany_stat: 'whenany' '(' ( ID ( ',' ID )* )? ')' body_stat=stat_block ( 'exits' '{' ( 'case'? ( ID | 'default' ) ':' stat_block )* '}' )? ;

whenever_stat: 'whenever' body_stat=stat_block ( 'exits' exit_stat=stat_block )? ;

implicit_throw_stat: 'throw' ';' ;

throw_stat: 'throw' expr ';' ;

try_catch_stat: 'try' try_stat=stat_block ( 'catch' ( '(' ID WideID? ')' )? stat_block )* ( 'finally' finally_stat=stat_block )? ;

break_stat: 'break' ';' ;

continue_stat: 'continue' ';' ;

redo_stat: 'redo' ';' ;

retry_stat: 'retry' ';' ;

return_stat: 'return' expr? ';' ;

clear_stat: 'clear' '(' ')' ';' ;

delay_stat: 'delay' '(' Number ')' ';' ;

assignment: ID '=' expr ';' ;

wideAssignment: wide_assign_expr ';' ;

wide_assign_expr: WideID ('+' | '-' | '*' | '/' | '%' | '&' | '|' | '^' | '??' )? '=' expr ;

internalAssignment: internal_assign_expr ';' ;

internal_assign_expr: InternalID ('+' | '-' | '*' | '/' | '%' | '&' | '|' | '^' | '??' )? '=' expr ;

memberAssignment: member_assign_expr ';' ;

member_assign_expr: ( objectAccess | arrayAccess ) ('+' | '-' | '*' | '/' | '%' | '&' | '|' | '^' | '??' )? '=' expr ;

variableDeletion: 'delete' (WideID | InternalID) ';' ;

print: 'print' '(' expr ( ',' ( ID | ( '#' Number ) ) )? ')' ';' ;

expr_stat: expr ';' ;

empty_stat: ';' ;


expr :  <assoc=right> expr '?' expr ':' expr
     | term ( ( '+' | '-' | '==' | '!=' | '<' | '<=' | '>' | '>=' | '&' | '|' | '&&' | '||' ) term )* 
     | op=( '-' | '!' ) expr
     | lambda
     | wide_assign_expr
     | internal_assign_expr
     ;

term : factor ( ( '*' | '/' | '%' | '??' ) factor )* 
     | ( WideID | InternalID ) op=( '++' | '--' )
     | op=( '++' | '--') ( WideID | InternalID )
     | inputCall
     | procCall
     | postcall
     | objectAccess
     | arrayAccess
     ;

factor : Number 
       | ID 
       | WideID
       | InternalID
       | BooleanLiteral 
       | WordTypeLiteral 
       | WordLiteral
       | RawWordLiteral
       | RawMultiWordLiteral
       | CharLiteral 
       | StringLiteral
       | RegexLiteral
       | arrayInitializer 
       | hashInitializer 
       | listMarker
       | methodCall
       | initCall
       | tupleCall
       | NullLiteral
       | ItemLiteral
       |'(' expr ')' 
       ;

inputCall : 'input' '(' ')' ;

methodCall : ( methodName=ID | MethodAlias ) ( openAngle='<' ( ID | '<' | '>' | ',' | '[' | ']' )* closeAngle='>' )? methodOpen='(' ( expr ( ',' expr )* )? methodClose=')' ;

procCall : ID '!' '(' ( expr ( ',' expr )* )? ')' ;

initCall : initToken='init' '..' ( ID | '<' | '>' | ',' | '[' | ']' )* initOpen='(' ( expr ( ',' expr )* )? initClose=')' ;

tupleCall : tupleToken='tuple' '..' tupleOpen='(' ( expr ( ',' expr )* )? tupleClose=')' ;

Number : ( '0' .. '9' )+ ( '.' ( '0' .. '9' )+ )? ;

CharLiteral : '\'' ( ~['] | EscapeSequence ) '\'' ;

StringLiteral : '"' ( ~["\n\r] | EscapeSequence )* '"' ;

RegexLiteral : '`' ( ~["\n\r] | EscapeSequence )* '`' ;

EscapeSequence : '\\' [btnfr"'\\] ;

listMarker : '@' ( 'NN' | 'NW' | 'TN' | 'TW' | 'PN' | 'PW' | '8X' | '6X' | 'D4' | '4X' | 'SO' ) ;

pair : factor '=' expr ;

BooleanLiteral : 'true' | 'false' ;

WordTypeLiteral : '$' [A-Z][a-z]* ;

WordLiteral : '/' ([ぁ-ゟ] | 'ー')+ ([a-zA-Z]*)? '/' ('w' | 'd') ;

RawWordLiteral: '/' ([ぁ-ゟ] | 'ー')+ WS* [ぁ-ヿ一-鿿]* ( WS* [ぁ-ヿ一-鿿]* )? '/r' ;

RawMultiWordLiteral: '/' ([ぁ-ゟ] | 'ー')+ ( WS* [ぁ-ヿ一-鿿]+ )* '/m' ;

NullLiteral : 'null' ;

ItemLiteral : ( '@item' | 'it' ) ;

lambda : ('(' ID ')' | ID) '=>' expr ;

arrayInitializer : '{' ( expr ( ',' expr )* )? '}' ;

hashInitializer : '%' '{' ( pair ( ',' pair )* )? '}' ;

ID: [a-zA-Z_][a-zA-Z0-9_]* ;

WideID: '&' ID ;

InternalID: '^' ID ;

MethodAlias : '\\' ( ID | '$' | '~' ) ;

objectAccess : factor '.' (ID | methodCall) ( objectAccessRest | arrayAccessRest | postcallRest )? ;

objectAccessRest : '.' (ID | methodCall) ( objectAccessRest | arrayAccessRest | postcallRest )? ;

arrayAccess : factor '[' expr ']' ( objectAccessRest | arrayAccessRest | postcallRest )? ;

arrayAccessRest : '[' expr ']' ( objectAccessRest | arrayAccessRest | postcallRest )? ;

postcall : ( factor | objectAccess | arrayAccess ) postcallOperator='..' methodCall ( objectAccessRest | arrayAccessRest | postcallRest )? ;

postcallRest : '..' methodCall ( objectAccessRest | arrayAccessRest | postcallRest )? ;

WS: [ \t\r\n　]+ -> skip;
Comment : '//' ~[\r\n]* -> skip ;
