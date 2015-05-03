#!/usr/bin/python
# -*- coding: utf-8 -*-

#分版本抽取克隆片段的度量值
#使用慈萌和李智超预处理结果

#使用方法
#命令+目录名1+目录名2+目录3
#extract_fragment_version.py path\emCRDFiles\blocks\  path\MAPFiles\blocks\ path\arff_result\

#结果以数字保存

import re, sys, os
import xml.dom.minidom
import math
import arff

#读取crd文件夹下的xml文件
clonefile_list = os.listdir(sys.argv[1])
mapfile_list = os.listdir(sys.argv[2])
outputpath = sys.argv[3]

#创建保存结果的目录
def mkdir(path):
    # 去除首位空格
    path=path.strip()
    # 去除尾部 \ 符号
    path=path.rstrip("\\")
 
    # 判断路径是否存在
    # 存在     True
    # 不存在   False
    isExists=os.path.exists(path)
 
    # 判断结果
    if not isExists:
        # 如果不存在则创建目录
        #print path+' 创建成功'
        # 创建目录操作函数
        os.makedirs(path)
        return True
    else:
        # 如果目录存在则不创建，并提示目录已存在
        #print path+' 目录已存在'
        return False

#编码转换 gb2312 -> utf-8
def convert_ecoding(file):
	f=open(file,'r').read()
	f=f.replace('<?xml version="1.0" encoding="gb2312"?>','<?xml version="1.0" encoding="utf-8"?>')
	f=unicode(f,encoding='gb2312').encode('utf-8')
	return f

#抽取第一个版本的克隆代码度量
Metric = []
Matrix = []
fileindex = 0
clonedoc = xml.dom.minidom.parse(sys.argv[1] + clonefile_list[fileindex]) 
cloneroot = clonedoc.documentElement              
source_nodes = cloneroot.getElementsByTagName('source')
#克隆度量提取	
#设置id
id = 0
for sourceNode in source_nodes:                
	#id = classid+CFid未实现，简单使用整数大计数
	id = id +1
	#克隆粒度
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
	ischange = 0
	changetimes = 0
	Metric =[id, lines, UOT, UOD, NOT, NOD, NOP, CTX, Clonelife, ischange, changetimes]
	Matrix.append(Metric)	

#将第一个版本的克隆组信息度量写入WEKA格式文件
mkdir(outputpath)
resultsfile = outputpath + clonefile_list[0] + '.arff'
arff.dump(resultsfile, Matrix, relation="clone_fragment_metrics", names=['id', 'lines', 'UOT', 'UOD', 'NOT', 'NOD', 'NOP','CTX', 'clonelife', 'ischange', 'changetimes'])


#后续版本的克隆片段度量提取
for i in range(1,len(clonefile_list)):

	clonedoc = xml.dom.minidom.parseString(convert_ecoding(sys.argv[1] + clonefile_list[i]))
	cloneroot = clonedoc.documentElement
	source_nodes = cloneroot.getElementsByTagName('source')

	Metric = []
	Matrix = []
	fileindex = fileindex + 1
	#克隆度量提取
	id = 0
	for sourceNode in source_nodes:                
		#id = classid+CFid未实现，简单使用整数计数
		id = id + 1
		#克隆粒度
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
		Metric =[id, lines, UOT, UOD, NOT, NOD, NOP, CTX]
			
		#提取克隆寿命和是否发生变化以及变化次数
		#clonelife and clonechangetime
		Metric.extend(clonelife, ischange, changetimes)
		Matrix.append(Metric)
	#将Matric写入文件中
	resultsfile = outputpath + clonefile_list[i] + '.arff'
	arff.dump(resultsfile, Matrix, relation="clone_fragment_metrics", names=['id', 'lines', 'UOT', 'UOD', 'NOT', 'NOD', 'NOP','CTX', 'clonelife', 'ischange', 'changetimes'])
print 'Succeed'