#!/usr/bin/perl

use warnings FATAL => 'all';
use lib qw(/home/perl_libs);
use strict;

use Data::UUID;



our $test;

use SQE_CGI qw(:standard );
use CGI::Carp;

use SQE_API::Worker;

my ($cgi, $error_ref) = SQE_CGI->new;

if ($cgi) {
    print SQE_API::Worker::process($cgi);


    $cgi->finish_output;
}



