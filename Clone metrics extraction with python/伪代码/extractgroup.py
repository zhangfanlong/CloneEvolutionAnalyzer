#!/usr/bin/python
# -*- coding: UTF-8 -*-

'''
#分版本抽取克隆组的度量值
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

#初始版本克隆度量提取
Metric = []
Matrix = []
clonedoc = xml.dom.minidom.parse(sys.argv[1] + clonefile_list[0]) 
cloneroot = clonedoc.documentElement
class_nodes = root.getElementsByTagName('class')	
#提取静态度量值
for classNode in class_nodes:               
	#克隆id
	classid = int(classNode.getAttribute('id'))
	classidlist.append(classid)
	#每个克隆群的克隆代码个数
	classnumber = int(classNode.getAttribute('nclones'))
	#Nicad的nlines
	classnlines = int(classNode.getAttribute('nlines'))
	#克隆相似度
	classsimilarity = int(classNode.getAttribute('similarity'))
	similarity = float(similarity) / 100 
	#文件分布samefile
	source_nodes = classNode.getElementsByTagName('source')
	for sourceNode in source_nodes:
		files.append(sourceNode.getAttribute('file'))
		samefile = '1' if len(set(files)) == 1 else '0'
	#设置只含有静态度量的克隆度量值
	pattern_static = '0'
	pattern_same = '0'
	pattern_add = '0'
	pattern_delete = '0'
	pattern_split = '0'
	pattern_inconsis = '0'
	pattern_consis = '0'				
	Metric.extend(classid, classnumber, classnlines, classsimilarity, samefile, pattern_static, pattern_same,pattern_add, pattern_delete, pattern_split, pattern_inconsis, pattern_consis, clonelife)
	Matrix.append(Metric)
#写入WEKA格式文件
resultsfile = sys.argv[1] + clonefile_list[0] + results
arff.dump(resultsfile, Matrix, relation="clone_metrics", names=['classid', 'classnumber', 'classnlines', 'classsimilarity', 'samefile', 'pattern_static', 'pattern_same','pattern_add', 'pattern_delete', 'pattern_split', 'pattern_inconsis', 'pattern_consis', 'clonelife'])

fileindexindex = 0 #指示器
for clonefile in clonefile_list[1,len(clonefile_list)]:
	Metric = []
	Matrix = []
	fileindex = fileindex + 1
	clonedoc = xml.dom.minidom.parse(sys.argv[1] + clonefile_list[fileindex]) 
	cloneroot = clonedoc.documentElement
	class_nodes = root.getElementsByTagName('class')	
	#克隆群个数
	nclone = len(class_nodes)
	#索引所有的克隆群
	classidlist = []
	#克隆群静态度量的提取提取静态度量值
	for classNode in class_nodes:               
		#克隆id
		classid = int(classNode.getAttribute('id'))
		classidlist.append(classid)
		#每个克隆群的克隆代码个数
		classnumber = int(classNode.getAttribute('nclones'))
		#Nicad的nlines
		classnlines = int(classNode.getAttribute('nlines'))
		#克隆相似度
		classsimilarity = int(classNode.getAttribute('similarity'))
		similarity = float(similarity) / 100 
		#文件分布
		source_nodes = classNode.getElementsByTagName('source')
		for sourceNode in source_nodes:
			files.append(sourceNode.getAttribute('file'))
			samefile = '1' if len(set(files)) == 1 else '0'
		#设置只含有静态度量的克隆度量值
		Metric.extend(classid, classnumber, classnlines, classsimilarity, samefile)
		#写入矩阵中
		Matrix.append(Metric)

	#映射的克隆群节点进化度量提取
	mapdoc = xml.dom.minidom.parse(sys.argv[2] + mapfile_list[fileindex-1]) 
	maproot = mapdoc.documentElement
	#映射的克隆群节点
	cgmap_nodes = maproot.getElementsByTagName('CGMap')	

	#提取克隆演化属性
	for cgmapnode in cgmap_nodes:
		epnode = cgmapnode.getElementsByTagName('EvolutionPattern')[0]
		#提取克隆模式
		if epnode.getAttribute('STATIC') == 'True':
			pattern_static = '1'
		else pattern_static = '0'
		if epnode.getAttribute('SAME') == 'True':
			pattern_same = '1'
		else pattern_same = '0'
		if epnode.getAttribute('ADD') == 'True':
			pattern_add = '1'
		else pattern_add = '0'
		if epnode.getAttribute('DELETE') == 'True':
			pattern_delete = '1'
		else pattern_delete = '0'
		if epnode.getAttribute('SPLIT') == 'True':
			pattern_split = '1'
		else pattern_split = '0'
		if epnode.getAttribute('INCONSISTENTCHANGE') == 'True':
			pattern_inconsis = '1'
		else pattern_inconsis = '0'
		if epnode.getAttribute('CONSISTENTCHANGE') == 'True':
			pattern_consis = '1'
		else pattern_consis = '0'				
		#回溯map文件并提取克隆寿命	
		clonelife = 1
		aimCGid = int(cgmap.getAttribute('srcCGid'))
		fileindextemp = fileindex 
		flag = 0
		dicttemp = {}
		#dowhile重写
		do
			fileindextemp--
			#寻找上一文件中是否有该克隆群
			mapdoctemp = xml.dom.minidom.parse(sys.argv[2] + mapfile_list[fileindextemp]) 
			maproottemp = mapdoctemp.documentElement
			cgmap_nodestemp = maproottemp.getElementsByTagName('CGMap')	
			dicttemp.clear()
			#生成srcCGid和destCGid字典
			for cgmapnodetemp in cgmap_nodestemp:	
				srcCGidtemp = cgmapnodetemp.getAttribute('srcCGid')
				destCGidtemp = cgmapnodetemp.getAttribute('destCGid')
				dicttemp.setdefault(destCGidtemp, srcCGidtemp)
			#字典中寻找 aimCGid
			if	aimCGid in dicttemp:
				flag = 1
				aimCGid = dicttemp[aimCGid]
			else flag = 0
			clonelife++
			while: fileindextemp != 0 && flag == 1
		#Matrix中找到响应的节点
		destCGid = cgmap.getAttribute('destCGid')
		i = classidlist.index(destCGid)
		#添加演化属性
		Matrix[i].append(pattern_static, pattern_same,pattern_add, pattern_delete, pattern_split, pattern_inconsis, pattern_consis, clonelife)
		
		#未映射克隆群节点进化度量设置
		unmappeddestcg_node = maproot.getElementsByTagName('UnMappedDestCG')
		unmappeddestcgnodes = unmappeddestcg_node.getElementsByTagName('CGInfo')
		for unmappeddestcfnode in unmappeddestcgnodes:
			destCGid = int(unmappeddestcfnode.getAttribute('id'))
			#设置克隆模式
			pattern_static = '0'
			pattern_same = '0'
			pattern_add = '0'
			pattern_delete = '0'
			pattern_split = '0'
			pattern_inconsis = '0'
			pattern_consis = '0'				
			#克隆寿命	
			age = 1
			#Matrix中找到响应的节点
			destCGid = cgmap.getAttribute('destCGid')
			i = classidlist.index(destCGid)
			#向特征向量中添加演化属性
			Matrix[i].append(pattern_static, pattern_same,pattern_add, pattern_delete, pattern_split, pattern_inconsis, pattern_consis, age)
	#将Matric写入文件中
	resultsfile = sys.argv[1] + clonefile_list[fileindex] + results
	arff.dump(resultsfile, Matrix, relation="clonegroup_metrics", names=['classid', 'classnumber', 'classnlines', 'classsimilarity', 'samefile', 'pattern_static', 'pattern_same','pattern_add', 'pattern_delete', 'pattern_split', 'pattern_inconsis', 'pattern_consis', 'clonelife'])