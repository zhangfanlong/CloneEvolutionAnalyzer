#!/usr/bin/python
# -*- coding: utf-8 -*-

#分版本抽取克隆组的度量值
#使用慈萌和李智超结果

#使用方法
#命令+目录名1+目录名2+目录3
#extract_group.py path\CRDFiles\blocks\  path\MAPFiles\blocks\ path\arff_result\

#结果以string类型的字面值保存

import re, sys, os
import xml.dom.minidom
import math
import arff

#初始版本克隆度量提取
Metric = []
Matrix = []

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

	
#提取第一个版本软件克隆组的静态度量值以及设置其演化度量值
clonedoc = xml.dom.minidom.parseString(convert_ecoding(sys.argv[1] + clonefile_list[0])) 
cloneroot = clonedoc.documentElement
class_nodes = cloneroot.getElementsByTagName('class')	

#克隆组的编号
classidlist=[] 
for classNode in class_nodes:               
	#克隆id
	classid = int(classNode.getAttribute('classid'))
	classidlist.append(classid)
	
	#每个克隆群的克隆代码个数
	classnumber = int(classNode.getAttribute('nclones'))
	
	#Nicad的nlines
	classnlines = int(classNode.getAttribute('nlines'))
	
	#克隆相似度
	classsimilarity = int(classNode.getAttribute('similarity'))
	similarity = float(classsimilarity) / 100 
	
	#文件分布samefile
	files = []
	source_nodes = classNode.getElementsByTagName('source')
	for sourceNode in source_nodes:
		files.append(sourceNode.getAttribute('file'))
		samefile = '1' if len(set(files)) == 1 else '0'
	#设置全部属性位置，并写入静态度量	
	Metric=[classid, classnumber, classnlines, similarity, samefile, '0', '0','0', '0', '0', '0', '0', '0']
	Matrix.append(Metric)
	
#将第一个版本的克隆组信息度量写入WEKA格式文件
mkdir(outputpath)
resultsfile = outputpath + clonefile_list[0] + '.arff'
arff.dump(resultsfile, Matrix, relation="clone_metrics", names=['classid', 'classnumber', 'classnlines', 'classsimilarity', 'samefile', 'pattern_static', 'pattern_same','pattern_add', 'pattern_delete', 'pattern_split', 'pattern_inconsis', 'pattern_consis', 'clonelife'])

for i in range(1,len(clonefile_list)):

	clonedoc = xml.dom.minidom.parseString(convert_ecoding(sys.argv[1] + clonefile_list[i]))
	cloneroot = clonedoc.documentElement
	class_nodes = cloneroot.getElementsByTagName('class')
	
	Matrix = []
	
	#索引所有的克隆群
	#classidlist = []	
	for classnode in class_nodes:	
		#克隆id
		classid = int(classnode.getAttribute('classid'))
		#classidlist.append(classid)
		
		#每个克隆群的克隆代码个数
		classnumber = int(classnode.getAttribute('nclones'))
		
		#Nicad的nlines
		classnlines = int(classnode.getAttribute('nlines'))
		
		#克隆相似度
		classsimilarity = int(classnode.getAttribute('similarity'))
		similarity = float(classsimilarity) / 100 
		
		#文件分布samefile
		source_nodes = classnode.getElementsByTagName('source')
		files = []
		for sourcenode in source_nodes:
			files.append(str(sourcenode.getAttribute('file')))
			samefile = '1' if len(set(files)) == 1 else '0'
		
		#设置只含有静态度量的克隆度量值
		Metric=[classid, classnumber, classnlines, similarity, samefile]
		#写入矩阵中
		Matrix.append(Metric)
		
		
	#映射的克隆群节点进化度量提取
	mapdoc = xml.dom.minidom.parse(sys.argv[2] + mapfile_list[i-1])
	maproot = mapdoc.documentElement
	cgmap_nodes = maproot.getElementsByTagName('CGMap')			
	
	#提取克隆演化属性
	for cgmapnode in cgmap_nodes:
		epnode = cgmapnode.getElementsByTagName('EvolutionPattern')[0]
		#提取克隆模式
		if str(epnode.getAttribute('STATIC')) == 'True':
			pattern_static = '1'
		else:pattern_static = '0'
		if str(epnode.getAttribute('SAME')) == 'True':
			pattern_same = '1'
		else:pattern_same = '0'
		if str(epnode.getAttribute('ADD')) == 'True':
			pattern_add = '1'
		else:pattern_add = '0'
		if str(epnode.getAttribute('DELETE')) == 'True':
			pattern_delete = '1'
		else:pattern_delete = '0'
		if str(epnode.getAttribute('SPLIT')) == 'True':
			pattern_split = '1'
		else:pattern_split = '0'
		if str(epnode.getAttribute('INCONSISTENTCHANGE')) == 'True':
			pattern_inconsis = '1'
		else:pattern_inconsis = '0'
		if str(epnode.getAttribute('CONSISTENTCHANGE')) == 'True':
			pattern_consis = '1'
		else:pattern_consis = '0'	
		#回溯map文件并提取克隆寿命	
		clonelife = 1
		aimCGid = int(cgmapnode.getAttribute('srcCGid'))
		fileindextemp = i - 1
		flag = 0
		while fileindextemp > 0 and flag == 0 :
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
			if	aimCGid in dicttemp :
				clonelife += 1
				aimCGid = dicttemp[aimCGid]
			else:flag = 1

		#Matrix中找到相对应的节点
		destCGid = int(cgmapnode.getAttribute('destCGid'))
		index = destCGid - 1
		#添加演化属性
		Matrix[index].extend([pattern_static, pattern_same,pattern_add, pattern_delete, pattern_split, pattern_inconsis, pattern_consis, clonelife])
		
	#未映射克隆群节点进化度量设置
	unmappeddestcg_nodes = maproot.getElementsByTagName('UnMappedDestCG')
	if len(unmappeddestcg_nodes) != 0:
		unmappeddestcg_node = unmappeddestcg_nodes[0]
		unmappeddestcgnodes = unmappeddestcg_node.getElementsByTagName('CGInfo')
		for unmappeddestcfnode in unmappeddestcgnodes:
			unmap_destCGid = int(unmappeddestcfnode.getAttribute('id'))
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
			index2 = unmap_destCGid - 1
			#向特征向量中添加演化属性
			Matrix[index2].extend([pattern_static, pattern_same,pattern_add, pattern_delete, pattern_split, pattern_inconsis, pattern_consis, age])
	#将Matric写入文件中
	resultsfile = outputpath + clonefile_list[i] + '.arff'
	arff.dump(resultsfile, Matrix, relation="clonegroup_metrics", names=['classid', 'classnumber', 'classnlines', 'classsimilarity', 'samefile', 'pattern_static', 'pattern_same','pattern_add', 'pattern_delete', 'pattern_split', 'pattern_inconsis', 'pattern_consis', 'clonelife'])
print 'Succeed'