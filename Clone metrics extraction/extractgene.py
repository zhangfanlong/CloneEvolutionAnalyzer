#!/usr/bin/python
# -*- coding: UTF-8 -*-

#作用：抽取克隆家系的度量值,使用慈萌和李智超结果
#使用方法:/usr/bin/python  pyname.py path1 path2
#结果:生成克隆家系的度量矩阵并写入arff文件中

import re, sys, os
import arff
import xml.etree.ElementTree as ET

#读取文件夹下的xml文件
file_list = os.listdir(sys.argv[1])

#统计克隆家系的个数
#number = len(file_list)

#抽取目录中每一个克隆家系的度量值
for filename in file_list:
	tree = ET.parse(sys.argv[1] + filename)
	root = tree.getroot()
	#获得属性信息
	#度量矩阵
	Maric []
	Dict1 = root[0].attrib 
	metric = Dict1.values()
	Dict2 = root[1].attrib
	metric = metric + Dict2.values()
	Maric.append(metics)#将度量值添加到度量矩阵中
#写入WEKA格式文件
arff.dump('clone_geneaology_result.arff', Maric, relation="clone_geneaology_matrics", names=['id', 'start', 'end', 'age', 'Static', 'same', 'add', 'delete', 'consistent' ,'inconsistent', 'split'])