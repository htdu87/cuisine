﻿using Cuisine.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace Cuisine.Controllers
{
    
    public class SearchController : Controller
    {
        private SparqlParameterizedString queryString;
        private LeviathanQueryProcessor process;
        private IDictionary dic;

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);

            string dsPath = Server.MapPath("~/App_Data/CusineThao.owl");
            //string dsPath = Server.MapPath("~/App_Data/db.owl");
            IGraph g = new Graph();
            g.LoadFromFile(dsPath);

            InMemoryDataset ds = new InMemoryDataset(g);
            process = new LeviathanQueryProcessor(ds);

            queryString = new SparqlParameterizedString();
            queryString.Namespaces.AddNamespace("owl", new Uri("http://www.w3.org/2002/07/owl#"));
            queryString.Namespaces.AddNamespace("rdf", new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#"));
            queryString.Namespaces.AddNamespace("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#"));
            queryString.Namespaces.AddNamespace("xsd", new Uri("http://www.w3.org/2001/XMLSchema#"));
            queryString.Namespaces.AddNamespace("cuisine", new Uri("http://www.semanticweb.org/latitude/ontologies/2021/1/untitled-ontology-12#"));
            //queryString.Namespaces.AddNamespace("cuisine", new Uri("http://www.semanticweb.org/du/ontologies/2021/1/cuisine#"));

            queryString.CommandText = "select * where {?x rdf:type cuisine:KhuVuc . ?x cuisine:coTenKhuVuc ?tenKV}";

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(queryString);

            SparqlResultSet resultSet = (SparqlResultSet)process.ProcessQuery(query);

            dic = new Dictionary<string, string>();

            foreach (SparqlResult result in resultSet)
            {
                IUriNode nodeX = (IUriNode)result.Value("x");
                ILiteralNode nodeTenKV = (ILiteralNode)result.Value("tenKV");

                dic[nodeX.Uri.Fragment.Remove(0, 1)] = nodeTenKV.Value;
            }
        }

        // GET: Search
        public ActionResult Index()
        {

            ViewBag.dsKhuVuc = dic;

            return View();
        }

        [HttpPost]
        public ActionResult Search(string keyword)
        {
            List<Dish> listDish = new List<Dish>();

            ViewBag.keyword = keyword;
            ViewBag.dsKhuVuc = dic;

            queryString.CommandText = "select * where {?x rdf:type cuisine:MonAn . ?x cuisine:thuocKhuVuc cuisine:"+keyword+ " ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay ; cuisine:coID ?id . }";

            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(queryString);

            SparqlResultSet resultSet = (SparqlResultSet)process.ProcessQuery(query);

            foreach (SparqlResult result in resultSet)
            {
                IUriNode nodeX = (IUriNode)result.Value("x");
                ILiteralNode nodeName = (ILiteralNode)result.Value("ten");
                ILiteralNode nodeDesc = (ILiteralNode)result.Value("mota");
                ILiteralNode nodePicture = (ILiteralNode)result.Value("anh");
                ILiteralNode nodeChay = (ILiteralNode)result.Value("chay");
                ILiteralNode nodeID = (ILiteralNode)result.Value("id");

                Dish dish = new Dish();
                dish.Desc = nodeDesc.Value;
                dish.Uri = nodeX.Uri.Fragment.Remove(0, 1);
                dish.Name = nodeName.Value;
                dish.Picture = nodePicture.Value;
                dish.Chay = nodeChay.Value == "true";
                dish.Id = long.Parse(nodeID.Value);

                listDish.Add(dish);
            }

            ViewBag.searchResult = listDish;

            return View();
        }

        public ActionResult Detail(int? id)
        {
            if(id != null)
            {
                queryString.CommandText = "select * where {?x rdf:type cuisine:MonAn . ?x cuisine:coID ?id ; cuisine:coTenMonAn ?ten ; cuisine:coMoTa ?mota ; cuisine:coAnh ?anh ; cuisine:laMonChay ?chay . FILTER(?id = " + id + ") }";

                SparqlQueryParser sparqlparser = new SparqlQueryParser();
                SparqlQuery query = sparqlparser.ParseFromString(queryString);

                SparqlResultSet resultSet = (SparqlResultSet)process.ProcessQuery(query);
                if(resultSet.Count>0)
                {
                    SparqlResult result = resultSet[0];

                    IUriNode nodeX = (IUriNode)result.Value("x");
                    ILiteralNode nodeName = (ILiteralNode)result.Value("ten");
                    ILiteralNode nodeDesc = (ILiteralNode)result.Value("mota");
                    ILiteralNode nodePicture = (ILiteralNode)result.Value("anh");
                    ILiteralNode nodeChay = (ILiteralNode)result.Value("chay");
                    ILiteralNode nodeID = (ILiteralNode)result.Value("id");

                    Dish dish = new Dish();
                    dish.Desc = nodeDesc.Value;
                    dish.Uri = nodeX.Uri.Fragment.Remove(0, 1);
                    dish.Name = nodeName.Value;
                    dish.Picture = nodePicture.Value;
                    dish.Chay = nodeChay.Value == "true";
                    dish.Id = long.Parse(nodeID.Value);

                    ViewBag.dish = dish;
                }

                List<ThanhPhan> congThuc = new List<ThanhPhan>();

                queryString.CommandText = "select ?tennl ?sl ?dvt where {?x rdf:type cuisine:MonAn . ?x cuisine:coID ?id . ?x cuisine:coThanhPhan ?tp . ?tp cuisine:coDVT ?dvt . ?tp cuisine:coSoLuong ?sl . ?tp cuisine:coNguyenLieu ?nl . ?nl cuisine:coTenNguyenLieu ?tennl . FILTER(?id = " + id+") }";
                query = sparqlparser.ParseFromString(queryString);
                resultSet = (SparqlResultSet)process.ProcessQuery(query);

                foreach (SparqlResult result in resultSet)
                {
                    ILiteralNode nodeTen = (ILiteralNode)result.Value("tennl");
                    ILiteralNode nodeSL = (ILiteralNode)result.Value("sl");
                    ILiteralNode nodeDVT = (ILiteralNode)result.Value("dvt");

                    ThanhPhan tp = new ThanhPhan();
                    tp.DVT = nodeDVT.Value;
                    tp.Ten = nodeTen.Value;
                    tp.SoLuong = float.Parse(nodeSL.Value);

                    congThuc.Add(tp);
                }

                ViewBag.congThuc = congThuc;
            }
            

            return View();
        }
    }
}