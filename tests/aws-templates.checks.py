"""Offline YAML, reference and dependency-cycle checks. Requires PyYAML.

This does not replace cfn-lint, AWS validate-template, or a deployment test.
"""
from pathlib import Path
from graphlib import TopologicalSorter
import re
import yaml


class CloudFormationLoader(yaml.SafeLoader):
    pass


def mapping(loader, node):
    result = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node)
        if key in result:
            raise ValueError(f"Duplicate YAML key: {key}")
        result[key] = loader.construct_object(value_node)
    return result


def intrinsic(loader, tag, node):
    value = (loader.construct_scalar(node) if isinstance(node, yaml.ScalarNode)
             else loader.construct_sequence(node) if isinstance(node, yaml.SequenceNode)
             else loader.construct_mapping(node))
    return {tag if tag == "Ref" else "Fn::" + tag: value}


CloudFormationLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, mapping)
CloudFormationLoader.add_multi_constructor("!", intrinsic)


def references(value):
    if isinstance(value, dict):
        if "Ref" in value:
            yield value["Ref"]
        if "Fn::GetAtt" in value:
            att = value["Fn::GetAtt"]
            yield att.split(".")[0] if isinstance(att, str) else att[0]
        if isinstance(value.get("Fn::Sub"), str):
            yield from (x.split(".")[0] for x in re.findall(r"\$\{([^!][^}]*)\}", value["Fn::Sub"]))
        for child in value.values():
            yield from references(child)
    elif isinstance(value, list):
        for child in value:
            yield from references(child)


for path in sorted(Path("deploy/aws").glob("*.yaml")):
    template = yaml.load(path.read_text(encoding="utf-8-sig"), Loader=CloudFormationLoader)
    resources = template["Resources"]
    parameters = template.get("Parameters", {})
    for ref in references(template):
        if not (ref.startswith("AWS::") or ref in resources or ref in parameters):
            raise ValueError(f"{path}: unknown reference {ref}")
    graph = {}
    for name, resource in resources.items():
        assert resource["Type"].startswith("AWS::"), f"{path}: missing resource type"
        depends = resource.get("DependsOn", [])
        if isinstance(depends, str):
            depends = [depends]
        if any(dependency not in resources for dependency in depends):
            raise ValueError(f"{path}: unknown explicit dependency")
        graph[name] = set(depends) | {ref for ref in references(resource) if ref in resources}
    list(TopologicalSorter(graph).static_order())
    print(f"PASS: {path} ({len(resources)} resources; valid YAML, references and dependency graph)")
