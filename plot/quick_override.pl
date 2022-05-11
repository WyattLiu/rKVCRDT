#!/usr/bin/perl
use strict;
use warnings;

while(<>){
	if($_ =~ /100/) {
		print "10 $_";
	}elsif($_ =~ /200/) {
		print "20 $_";
	}elsif($_ =~ /300/) {
		print "30 $_";
	}elsif($_ =~ /400/) {
		print "40 $_";
	}elsif($_ =~ /500/) {
		print "50 $_";
	}elsif($_ =~ /baseline/) {
		print "0,10,20,30,40,50 $_";
	}elsif($_ =~ /\w0\w/) {
	     	print "0 $_"  
	}else{
		print "# error auto override for $_"
	}
}
