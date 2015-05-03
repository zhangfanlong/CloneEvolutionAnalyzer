#!/usr/bin/python
# -*- coding: UTF-8 -*-

'''
#分版本抽取克隆片段的度量值
#使用慈萌和李智超结果
#使用方法
#命令+目录名1+目录名2
#C:\Users\founder\extract.py E:\论文资料\5实验系统\wget-results\CRDFiles\blocks\ E:\论文资料\5实验系统\wget-results\MAPFiles\blocks\
'''
import re, sys, os
import xml.dom.minidom
import math

#读取crd文件夹下的xml文件
clonefile_list = os.listdir(sys.argv[1])
mapfile_list = os.listdir(sys.argv[2])


#抽取第一个版本的克隆代码度量
Metric = []
Matrix = []
fileindex = 0
clonedoc = xml.dom.minidom.parse(sys.argv[1] + clonefile_list[fileindex]) 
cloneroot = clonedoc.documentElement              
source_nodes = cloneroot.getElementsByTagName('source')
#克隆度量提取	
for sourceNode in source_nodes:                
	#克隆粒度
	#id = classid+CFid
	e = int(sourceNode.getAttribute('endline'))
	s = int(sourceNode.getAttribute('startline'))	
	lines = e - s
	#Halstead度量
	uotNode = sourceNode.getElementsByTagName('UniqueOprator')[0]
	uotvalue = int(uotNode.childNodes[0].nodeValue)
	UOT = str(uotvalue))
	uodNode = sourceNode.getElementsByTagName('UniqueOprand')[0]
	uodvalue = int(uodNode.childNodes[0].nodeValue)
	UOD = str(uodvalue)
	notNode = sourceNode.getElementsByTagName('TotalOprator')[0]
	notvalue = int(notNode.childNodes[0].nodeValue)
	NOT = str(notvalue)
	nodNode = sourceNode.getElementsByTagName('TotalOprand')[0]
	nodvalue = int(nodNode.childNodes[0].nodeValue)
	NOD = str(nodvalue))		
	#参数个数
	if sourceNode.getElementsByTagName('methodInfo') == []:
		NOP = '0'
	else:
		NOP = sourceNode.getElementsByTagName('methodInfo')[0].getAttribute('mParaNum')
	#上下文信息，默认为定义
	context = 'DEF'
	flag = 1 if sourceNode.getElementsByTagName('blockInfo') == [] else 0
	if flag == 0:
		btype_nodes = sourceNode.getElementsByTagName('bType')
		l = len(btype_nodes)
		btype_real = btype_nodes[l - 1]
		context = btype_real.childNodes[0].nodeValue
		CTX = context
	#提取克隆寿命和是否发生变化
	clonelife = 1
	clonechange = 0
	Metric.extend(id, lines, UOT, UOD, NOT, NOD, pattern_same,pattern_add, pattern_delete, pattern_split, pattern_inconsis, pattern_consis, clonelife, clonechange)
	Matrix.append(Metric)	
#将Matric写入文件中
resultsfile = sys.argv[1] + clonefile_list[fileindex] + results
arff.dump(resultsfile, Matrix, relation="clonegroup_metrics", names=['classid', 'classnumber', 'classnlines', 'classsimilarity', 'samefile', 'pattern_static', 'pattern_same','pattern_add', 'pattern_delete', 'pattern_split', 'pattern_inconsis', 'pattern_consis', 'clonelife'])



#后续版本的克隆片段度量提取
for clonefile in clonefile_list[1,len(clonefile_list)]:
	Metric = []
	Matrix = []
	fileindex = fileindex + 1
	clonedoc = xml.dom.minidom.parse(sys.argv[1] + clonefile_list[fileindex]) 
	cloneroot = clonedoc.documentElement              
	source_nodes = cloneroot.getElementsByTagName('source')
	#克隆度量提取
	CFID
	for sourceNode in source_nodes:                
		#克隆粒度
		#id = classid+CFid
		e = int(sourceNode.getAttribute('endline'))
		s = int(sourceNode.getAttribute('startline'))	
		lines = e - s
		#Halstead度量
		uotNode = sourceNode.getElementsByTagName('UniqueOprator')[0]
		uotvalue = int(uotNode.childNodes[0].nodeValue)
		UOT = str(uotvalue))
		uodNode = sourceNode.getElementsByTagName('UniqueOprand')[0]
		uodvalue = int(uodNode.childNodes[0].nodeValue)
		UOD = str(uodvalue)
		notNode = sourceNode.getElementsByTagName('TotalOprator')[0]
		notvalue = int(notNode.childNodes[0].nodeValue)
		NOT = str(notvalue)
		nodNode = sourceNode.getElementsByTagName('TotalOprand')[0]
		nodvalue = int(nodNode.childNodes[0].nodeValue)
		NOD = str(nodvalue))
		
		#参数个数
		NOP
		if sourceNode.getElementsByTagName('methodInfo') == []:
			NOP = '0'
		else:
			NOP = sourceNode.getElementsByTagName('methodInfo')[0].getAttribute('mParaNum')
		#上下文信息，默认为定义
		context = 'DEF'
		flag = 1 if sourceNode.getElementsByTagName('blockInfo') == [] else 0
		if flag == 0:
			btype_nodes = sourceNode.getElementsByTagName('bType')
			l = len(btype_nodes)
			btype_real = btype_nodes[l - 1]
			context = btype_real.childNodes[0].nodeValue
			CTX = context
		Metric.extend(id, lines, UOT, UOD, NOT, NOD, pattern_same,pattern_add, pattern_delete, pattern_split, pattern_inconsis, pattern_consis, clonelife)
		Matrix.append(Metric)	
	#提取克隆寿命和是否发生变化
	#clonelife and clonechangetime
	#将Matric写入文件中
	resultsfile = sys.argv[1] + clonefile_list[fileindex] + results
	arff.dump(resultsfile, Matrix, relation="clonegroup_metrics", names=['classid', 'classnumber', 'classnlines', 'classsimilarity', 'samefile', 'pattern_static', 'pattern_same','pattern_add', 'pattern_delete', 'pattern_split', 'pattern_inconsis', 'pattern_consis', 'clonelife'])