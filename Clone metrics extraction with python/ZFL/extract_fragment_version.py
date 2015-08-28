#!/usr/bin/python
# -*- coding: utf-8 -*-

#分版本抽取克隆片段的度量值
#使用慈萌和李智超预处理结果

#使用方法
#命令+目录名1+目录名2+目录3
#extract_fragment_version.py path\emCRDFiles\blocks\  path\MAPFiles\blocks\  path\arff_result\

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
# def mkdir(path):
    # # 去除首位空格
    # path=path.strip()
    # # 去除尾部 \ 符号
    # path=path.rstrip("\\")
 
    # # 判断路径是否存在
    # # 存在     True
    # # 不存在   False
    # isExists=os.path.exists(path)
 
    # # 判断结果
    # if not isExists:
        # # 如果不存在则创建目录
        # #print path+' 创建成功'
        # # 创建目录操作函数
        # os.makedirs(path)
        # return True
    # else:
        # # 如果目录存在则不创建，并提示目录已存在
        # #print path+' 目录已存在'
        # return False

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
	id = id + 1
	#id = int(sourceNode.getAttribute('pcid'))
	#克隆粒度
	e = int(sourceNode.getAttribute('endline'))
	s = int(sourceNode.getAttribute('startline'))	
	lines = e - s
	#Halstead度量
	uotNode = sourceNode.getElementsByTagName('UniqueOprator')[0]
	uotvalue = int(uotNode.childNodes[0].nodeValue)
	UOT = str(uotvalue)
	uodNode = sourceNode.getElementsByTagName('UniqueOprand')[0]
	uodvalue = int(uodNode.childNodes[0].nodeValue)
	UOD = str(uodvalue)
	notNode = sourceNode.getElementsByTagName('TotalOprator')[0]
	notvalue = int(notNode.childNodes[0].nodeValue)
	NOT = str(notvalue)
	nodNode = sourceNode.getElementsByTagName('TotalOprand')[0]
	nodvalue = int(nodNode.childNodes[0].nodeValue)
	NOD = str(nodvalue)
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
	Metric =[id, lines, UOT, UOD, NOT, NOD, NOP, CTX, clonelife, ischange, changetimes]
	Matrix.append(Metric)	

#将第一个版本的克隆组信息度量写入WEKA格式文件
# mkdir(outputpath)
resultsfile = outputpath + clonefile_list[0] + '.arff'
arff.dump(resultsfile, Matrix, relation="clone_fragment_metrics", names=['id', 'lines', 'UOT', 'UOD', 'NOT', 'NOD', 'NOP','CTX', 'clonelife', 'ischange', 'changetimes'])

########################################################################

#后续版本的克隆片段度量提取
for i in range(1,len(clonefile_list)):

	clonedoc = xml.dom.minidom.parseString(convert_ecoding(sys.argv[1] + clonefile_list[i]))
	cloneroot = clonedoc.documentElement
	class_nodes = cloneroot.getElementsByTagName('class')
	# 创建class对应字典
	classdict = {}
	for classNode in class_nodes:
		classid = int(classNode.getAttribute('classid'))
		nclones = int(classNode.getAttribute('nclones'))
		classdict.setdefault(classid,nclones)
	
	source_nodes = cloneroot.getElementsByTagName('source')

	Matrix = []
	fileindex = fileindex + 1
	#克隆度量提取
	id = 0
	for sourceNode in source_nodes:                
		#id = classid+CFid未实现，简单使用整数计数
		id = id + 1
		#id = int(sourceNode.getAttribute('pcid'))
		#克隆粒度
		e = int(sourceNode.getAttribute('endline'))
		s = int(sourceNode.getAttribute('startline'))	
		lines = e - s
		#Halstead度量
		uotNode = sourceNode.getElementsByTagName('UniqueOprator')[0]
		uotvalue = int(uotNode.childNodes[0].nodeValue)
		UOT = str(uotvalue)
		uodNode = sourceNode.getElementsByTagName('UniqueOprand')[0]
		uodvalue = int(uodNode.childNodes[0].nodeValue)
		UOD = str(uodvalue)
		notNode = sourceNode.getElementsByTagName('TotalOprator')[0]
		notvalue = int(notNode.childNodes[0].nodeValue)
		NOT = str(notvalue)
		nodNode = sourceNode.getElementsByTagName('TotalOprand')[0]
		nodvalue = int(nodNode.childNodes[0].nodeValue)
		NOD = str(nodvalue)
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
		Metric = [id, lines, UOT, UOD, NOT, NOD, NOP, CTX]
		#写入矩阵中
		Matrix.append(Metric)
		
	#提取克隆寿命和是否发生变化以及变化次数
		
	#映射的克隆群节点进化度量提取
	mapdoc = xml.dom.minidom.parse(sys.argv[2] + mapfile_list[i - 1])
	maproot = mapdoc.documentElement
	#未映射克隆组
	unmappeddestcg_nodes = maproot.getElementsByTagName('UnMappedDestCG')
	if len(unmappeddestcg_nodes) != 0:
		unmappeddestcg_node = unmappeddestcg_nodes[0]
		unmappeddestcgnodes = unmappeddestcg_node.getElementsByTagName('CGInfo')
		for node in unmappeddestcgnodes:
			unmap_destCGid = int(node.getAttribute('id'))	
			size = int(node.getAttribute('size'))
			#克隆寿命	
			clonelife = 1
			ischange = 0 #没改变
			changetimes = 0 
			#找到响应节点
			index = 0
			for key in classdict:
				if int(key) < unmap_destCGid:
					index += classdict[key]
			#print index
			#向特征向量中添加演化属性
			while size > 0:
				Matrix[index].extend([clonelife, ischange, changetimes])
				index += 1
				size -= 1
	
	#已映射克隆组			
	cgmap_nodes = maproot.getElementsByTagName('CGMap')	
	for cgmap_node in cgmap_nodes:          #group
		#clonelife / ischange  / changetimes
		aimCGid = int(cgmap_node.getAttribute('srcCGid'))#classid
		CGid = int(cgmap_node.getAttribute('destCGid'))
		CGSize = int(cgmap_node.getAttribute('destCGsize'))
		
		cfmap_nodes = cgmap_node.getElementsByTagName('CFMap')
		CFSize = len(cfmap_nodes)
		CFidTemp = []
		
		
		for cfmap_node in cfmap_nodes:                 #fragment
			clonelife = 1
			changetimes = 0
			fileindextemp = i - 1
			flag = 0
			fflag = 0
			aimCFid = int(cfmap_node.getAttribute('srcCFid'))
			CFid = int(cfmap_node.getAttribute('destCFid'))
			CFidTemp.append(CFid)
			
			while fileindextemp > 0 and flag == 0 and fflag == 0:
				fileindextemp -= 1
				#寻找上一文件中是否有该克隆群
				mapdoctemp = xml.dom.minidom.parse(sys.argv[2] + mapfile_list[fileindextemp]) 
				maproottemp = mapdoctemp.documentElement
				cgmap_nodestemp = maproottemp.getElementsByTagName('CGMap')	
				dicttemp = {}
				#生成srcCGid和destCGid字典
				for cgmapnodetemp in cgmap_nodestemp:	
					srcCGidtemp = int(cgmapnodetemp.getAttribute('srcCGid'))
					destCGidtemp = int(cgmapnodetemp.getAttribute('destCGid'))
					dicttemp.setdefault(destCGidtemp, srcCGidtemp)
				#字典中寻找 aimCGid
				if aimCGid in dicttemp :
					for temp in cgmap_nodestemp:
						if int(temp.getAttribute('destCGid')) == aimCGid: #确定哪一个节点是对应aimCGid的
							fflag = 1
							cfmap_nodestemps = temp.getElementsByTagName('CFMap')
							#if len(cfmap_nodestemps) == 0:
								
							for cfmap_nodetemp in cfmap_nodestemps:
								if int(cfmap_nodetemp.getAttribute('destCFid')) == aimCFid:
									clonelife += 1
									fflag = 0
									if cfmap_nodetemp.getAttribute('textSim') != "1":
										changetimes += 1
									aimCFid = int(cfmap_nodetemp.getAttribute('srcCFid'))
									aimCGid = dicttemp[aimCGid]
									break
							break
				else:flag = 1
			
			if changetimes == 0:
				ischange = 0	#没发生变化
			else:ischange = 1		
			
			#找到响应节点
			index = 0
			for key in classdict:
				if int(key) < CGid:
					index += classdict[key]				
			index += CFid - 1 	#加上片段映射，从0开始减一
			Matrix[index].extend([clonelife, ischange, changetimes])
		
		if	CGSize > CFSize:
			index = 0	
			for key in classdict:
				if int(key) < CGid:
					index += classdict[key]
			for cfindex in range(1,CGSize + 1):
				if cfindex not in CFidTemp:
					index = index + cfindex - 1 
					Matrix[index].extend([1, 0, 0]) #新添加的
					index = index - cfindex + 1
		
					
		
	#将Matric写入文件中
	resultsfile = outputpath + clonefile_list[i] + '.arff'
	arff.dump(resultsfile, Matrix, relation="clone_fragment_metrics", names=['id', 'lines', 'UOT', 'UOD', 'NOT', 'NOD', 'NOP','CTX', 'clonelife', 'ischange', 'changetimes'])
print 'Succeed'