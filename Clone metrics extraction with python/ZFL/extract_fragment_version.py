#!/usr/bin/python
# -*- coding: utf-8 -*-

#�ְ汾��ȡ��¡Ƭ�εĶ���ֵ
#ʹ�ô��Ⱥ����ǳ�Ԥ������

#ʹ�÷���
#����+Ŀ¼��1+Ŀ¼��2+Ŀ¼3
#extract_fragment_version.py path\emCRDFiles\blocks\  path\MAPFiles\blocks\  path\arff_result\

#��������ֱ���

import re, sys, os
import xml.dom.minidom
import math
import arff

#��ȡcrd�ļ����µ�xml�ļ�
clonefile_list = os.listdir(sys.argv[1])
mapfile_list = os.listdir(sys.argv[2])
outputpath = sys.argv[3]

#������������Ŀ¼
# def mkdir(path):
    # # ȥ����λ�ո�
    # path=path.strip()
    # # ȥ��β�� \ ����
    # path=path.rstrip("\\")
 
    # # �ж�·���Ƿ����
    # # ����     True
    # # ������   False
    # isExists=os.path.exists(path)
 
    # # �жϽ��
    # if not isExists:
        # # ����������򴴽�Ŀ¼
        # #print path+' �����ɹ�'
        # # ����Ŀ¼��������
        # os.makedirs(path)
        # return True
    # else:
        # # ���Ŀ¼�����򲻴���������ʾĿ¼�Ѵ���
        # #print path+' Ŀ¼�Ѵ���'
        # return False

#����ת�� gb2312 -> utf-8
def convert_ecoding(file):
	f=open(file,'r').read()
	f=f.replace('<?xml version="1.0" encoding="gb2312"?>','<?xml version="1.0" encoding="utf-8"?>')
	f=unicode(f,encoding='gb2312').encode('utf-8')
	return f

#��ȡ��һ���汾�Ŀ�¡�������
Metric = []
Matrix = []
fileindex = 0
clonedoc = xml.dom.minidom.parse(sys.argv[1] + clonefile_list[fileindex]) 
cloneroot = clonedoc.documentElement              
source_nodes = cloneroot.getElementsByTagName('source')
#��¡������ȡ	
#����id
id = 0
for sourceNode in source_nodes:                
	#id = classid+CFidδʵ�֣���ʹ�����������
	id = id + 1
	#id = int(sourceNode.getAttribute('pcid'))
	#��¡����
	e = int(sourceNode.getAttribute('endline'))
	s = int(sourceNode.getAttribute('startline'))	
	lines = e - s
	#Halstead����
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
	#��������
	if sourceNode.getElementsByTagName('methodInfo') == []:
		NOP = '0'
	else:
		NOP = sourceNode.getElementsByTagName('methodInfo')[0].getAttribute('mParaNum')
	#��������Ϣ��Ĭ��Ϊ����
	context = 'DEF'
	flag = 1 if sourceNode.getElementsByTagName('blockInfo') == [] else 0
	if flag == 0:
		btype_nodes = sourceNode.getElementsByTagName('bType')
		l = len(btype_nodes)
		btype_real = btype_nodes[l - 1]
		context = btype_real.childNodes[0].nodeValue
	CTX = context
	#��ȡ��¡�������Ƿ����仯
	clonelife = 1
	ischange = 0
	changetimes = 0
	Metric =[id, lines, UOT, UOD, NOT, NOD, NOP, CTX, clonelife, ischange, changetimes]
	Matrix.append(Metric)	

#����һ���汾�Ŀ�¡����Ϣ����д��WEKA��ʽ�ļ�
# mkdir(outputpath)
resultsfile = outputpath + clonefile_list[0] + '.arff'
arff.dump(resultsfile, Matrix, relation="clone_fragment_metrics", names=['id', 'lines', 'UOT', 'UOD', 'NOT', 'NOD', 'NOP','CTX', 'clonelife', 'ischange', 'changetimes'])

########################################################################

#�����汾�Ŀ�¡Ƭ�ζ�����ȡ
for i in range(1,len(clonefile_list)):

	clonedoc = xml.dom.minidom.parseString(convert_ecoding(sys.argv[1] + clonefile_list[i]))
	cloneroot = clonedoc.documentElement
	class_nodes = cloneroot.getElementsByTagName('class')
	# ����class��Ӧ�ֵ�
	classdict = {}
	for classNode in class_nodes:
		classid = int(classNode.getAttribute('classid'))
		nclones = int(classNode.getAttribute('nclones'))
		classdict.setdefault(classid,nclones)
	
	source_nodes = cloneroot.getElementsByTagName('source')

	Matrix = []
	fileindex = fileindex + 1
	#��¡������ȡ
	id = 0
	for sourceNode in source_nodes:                
		#id = classid+CFidδʵ�֣���ʹ����������
		id = id + 1
		#id = int(sourceNode.getAttribute('pcid'))
		#��¡����
		e = int(sourceNode.getAttribute('endline'))
		s = int(sourceNode.getAttribute('startline'))	
		lines = e - s
		#Halstead����
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
		#��������
		if sourceNode.getElementsByTagName('methodInfo') == []:
			NOP = '0'
		else:
			NOP = sourceNode.getElementsByTagName('methodInfo')[0].getAttribute('mParaNum')
		#��������Ϣ��Ĭ��Ϊ����
		context = 'DEF'
		flag = 1 if sourceNode.getElementsByTagName('blockInfo') == [] else 0
		if flag == 0:
			btype_nodes = sourceNode.getElementsByTagName('bType')
			l = len(btype_nodes)
			btype_real = btype_nodes[l - 1]
			context = btype_real.childNodes[0].nodeValue
		CTX = context
		Metric = [id, lines, UOT, UOD, NOT, NOD, NOP, CTX]
		#д�������
		Matrix.append(Metric)
		
	#��ȡ��¡�������Ƿ����仯�Լ��仯����
		
	#ӳ��Ŀ�¡Ⱥ�ڵ����������ȡ
	mapdoc = xml.dom.minidom.parse(sys.argv[2] + mapfile_list[i - 1])
	maproot = mapdoc.documentElement
	#δӳ���¡��
	unmappeddestcg_nodes = maproot.getElementsByTagName('UnMappedDestCG')
	if len(unmappeddestcg_nodes) != 0:
		unmappeddestcg_node = unmappeddestcg_nodes[0]
		unmappeddestcgnodes = unmappeddestcg_node.getElementsByTagName('CGInfo')
		for node in unmappeddestcgnodes:
			unmap_destCGid = int(node.getAttribute('id'))	
			size = int(node.getAttribute('size'))
			#��¡����	
			clonelife = 1
			ischange = 0 #û�ı�
			changetimes = 0 
			#�ҵ���Ӧ�ڵ�
			index = 0
			for key in classdict:
				if int(key) < unmap_destCGid:
					index += classdict[key]
			#print index
			#����������������ݻ�����
			while size > 0:
				Matrix[index].extend([clonelife, ischange, changetimes])
				index += 1
				size -= 1
	
	#��ӳ���¡��			
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
				#Ѱ����һ�ļ����Ƿ��иÿ�¡Ⱥ
				mapdoctemp = xml.dom.minidom.parse(sys.argv[2] + mapfile_list[fileindextemp]) 
				maproottemp = mapdoctemp.documentElement
				cgmap_nodestemp = maproottemp.getElementsByTagName('CGMap')	
				dicttemp = {}
				#����srcCGid��destCGid�ֵ�
				for cgmapnodetemp in cgmap_nodestemp:	
					srcCGidtemp = int(cgmapnodetemp.getAttribute('srcCGid'))
					destCGidtemp = int(cgmapnodetemp.getAttribute('destCGid'))
					dicttemp.setdefault(destCGidtemp, srcCGidtemp)
				#�ֵ���Ѱ�� aimCGid
				if aimCGid in dicttemp :
					for temp in cgmap_nodestemp:
						if int(temp.getAttribute('destCGid')) == aimCGid: #ȷ����һ���ڵ��Ƕ�ӦaimCGid��
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
				ischange = 0	#û�����仯
			else:ischange = 1		
			
			#�ҵ���Ӧ�ڵ�
			index = 0
			for key in classdict:
				if int(key) < CGid:
					index += classdict[key]				
			index += CFid - 1 	#����Ƭ��ӳ�䣬��0��ʼ��һ
			Matrix[index].extend([clonelife, ischange, changetimes])
		
		if	CGSize > CFSize:
			index = 0	
			for key in classdict:
				if int(key) < CGid:
					index += classdict[key]
			for cfindex in range(1,CGSize + 1):
				if cfindex not in CFidTemp:
					index = index + cfindex - 1 
					Matrix[index].extend([1, 0, 0]) #����ӵ�
					index = index - cfindex + 1
		
					
		
	#��Matricд���ļ���
	resultsfile = outputpath + clonefile_list[i] + '.arff'
	arff.dump(resultsfile, Matrix, relation="clone_fragment_metrics", names=['id', 'lines', 'UOT', 'UOD', 'NOT', 'NOD', 'NOP','CTX', 'clonelife', 'ischange', 'changetimes'])
print 'Succeed'