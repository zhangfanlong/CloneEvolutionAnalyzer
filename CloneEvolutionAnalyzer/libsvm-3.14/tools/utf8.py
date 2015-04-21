#!/usr/bin/python
# -*- coding: cp936 -*-
import re, sys, os
#C:\Users\founder\utf8.py E:\dnsjava-optimal-results\CRDFiles\blocks\
#C:\Users\founder\utf8.py E:\dnsjava-optimal-results\MAPFiles\blocks\
#C:\Users\founder\utf8.py E:\jEdit-results\CRDFiles\blocks\
#C:\Users\founder\utf8.py E:\wget-results\CRDFiles\blocks\
#C:\Users\founder\utf8.py E:\itextsharp-results\CRDFiles\blocks\
#C:\Users\founder\utf8.py E:\conky-results\CRDFiles\blocks\
pattern = re.compile('.*encoding=.*')
file_list = os.listdir(sys.argv[1])
for filename in file_list:
    file_object_read = open(sys.argv[1] + filename, 'r')
    stringsave = ""
    stringread = file_object_read.readline()
    while stringread:
        stringread = stringread.replace('gb2312', 'utf-8')
        stringsave = stringsave + stringread
        stringread = file_object_read.readline()
    file_object_save = open(sys.argv[1] + filename, 'w')
    file_object_save.write(stringsave)
    file_object_read.close()
    file_object_save.close()
    
